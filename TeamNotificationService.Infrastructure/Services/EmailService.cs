using System.Net;
using System.Net.Mail;
using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using TeamNotificationService.Application.Interfaces.Services.Email;
using TeamNotificationService.Application.Models.Email;
using TeamNotificationService.Infrastructure.Options;

namespace TeamNotificationService.Infrastructure.Services;

public sealed class EmailService : BackgroundService, IEmailService
{
    private readonly Channel<SendEmailToRecipientRequestModel> _emailChannel;
    private readonly Serilog.ILogger _logger;
    private readonly EmailOptions _options;

    public EmailService(Serilog.ILogger logger, IOptions<EmailOptions> options)
    {
        _logger = logger;
        _options = options.Value;
        _emailChannel = Channel.CreateBounded<SendEmailToRecipientRequestModel>(
            new BoundedChannelOptions(_options.QueueCapacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false,
                AllowSynchronousContinuations = false
            });
    }

    public Task QueueEmailAsync(SendEmailToRecipientRequestModel message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(message.ToEmail);
        ArgumentException.ThrowIfNullOrWhiteSpace(message.Subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(message.Body);

        return _emailChannel.Writer.WriteAsync(message, cancellationToken).AsTask();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in _emailChannel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await SendEmailAsync(message, stoppingToken);
                _logger.Information("Email sent successfully to {Recipient}", message.ToEmail);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Unable to send email to {Recipient}", message.ToEmail);
            }
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _emailChannel.Writer.TryComplete();
        return base.StopAsync(cancellationToken);
    }

    private async Task SendEmailAsync(SendEmailToRecipientRequestModel message, CancellationToken cancellationToken)
    {
        var fromEmail = message.FromEmail ?? _options.DefaultFromEmail;
        var fromName = message.FromName ?? _options.DefaultFromName;
        using var mailMessage = new MailMessage
        {
            From = new MailAddress(fromEmail, fromName),
            Subject = message.Subject,
            Body = message.Body,
            IsBodyHtml = message.IsBodyHtml
        };

        mailMessage.To.Add(message.ToEmail);

        using var smtpClient = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.UseSsl,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(_options.Username, _options.Password)
        };

        await smtpClient.SendMailAsync(mailMessage, cancellationToken);
    }
}
