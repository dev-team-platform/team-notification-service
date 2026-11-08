using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Threading.Channels;
using TeamNotificationService.Infrastructure.Options;

namespace TeamNotificationService.Infrastructure.Services.RabbitMq;

public sealed class RabbitMqPublisherHostedService : BackgroundService
{
    private readonly RabbitMqConnection _rabbitMqConnection;
    private readonly RabbitMqTopologyInitializer _topologyInitializer;
    private readonly IOptions<RabbitMqOptions> _rabbitMqOptions;
    private readonly Serilog.ILogger _logger;

    private readonly Channel<RabbitMqPublishRequest> _publishChannel;

    public RabbitMqPublisherHostedService(
        RabbitMqConnection rabbitMqConnection,
        RabbitMqTopologyInitializer topologyInitializer,
        IOptions<RabbitMqOptions> rabbitMqOptions,
        Serilog.ILogger logger)
    {
        _rabbitMqConnection = rabbitMqConnection;
        _topologyInitializer = topologyInitializer;
        _rabbitMqOptions = rabbitMqOptions;
        _logger = logger;

        _publishChannel = Channel.CreateBounded<RabbitMqPublishRequest>(new BoundedChannelOptions(5000)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        });
    }

    public async Task PublishAsync(
        string publisherName,
        string routingKey,
        ReadOnlyMemory<byte> body,
        BasicProperties properties,
        CancellationToken cancellationToken = default)
    {
        if (!_rabbitMqOptions.Value.Publishers.TryGetValue(publisherName, out var publisher))
        {
            throw new ArgumentException(
                $"RabbitMQ publisher '{publisherName}' is not configured.",
                nameof(publisherName));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(routingKey);

        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var request = new RabbitMqPublishRequest(
            publisher.Exchange,
            routingKey,
            body,
            properties,
            completion);

        await _publishChannel.Writer.WriteAsync(request, cancellationToken);
        await completion.Task.WaitAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _topologyInitializer.InitializeAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunPublisherAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.Warning(
                    exception,
                    "RabbitMQ publisher disconnected; reconnecting");

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task RunPublisherAsync(CancellationToken stoppingToken)
    {
        var connection = await _rabbitMqConnection.GetConnectionAsync(stoppingToken);

        await using var channel = await connection.CreateChannelAsync(
                new CreateChannelOptions(
                    publisherConfirmationsEnabled: true,
                    publisherConfirmationTrackingEnabled: true),
                stoppingToken);

        await foreach (var request in _publishChannel.Reader.ReadAllAsync(stoppingToken))
        {
            await PublishCoreAsync(
                channel,
                request,
                stoppingToken);
        }
    }

    private async Task PublishCoreAsync(
        IChannel channel,
        RabbitMqPublishRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await channel.BasicPublishAsync(
                exchange: request.Exchange,
                routingKey: request.RoutingKey,
                mandatory: true,
                basicProperties: request.Properties,
                body: request.Body,
                cancellationToken: cancellationToken);

            request.Completion.TrySetResult();
        }
        catch (Exception exception)
        {
            request.Completion.TrySetException(exception);

            throw;
        }
    }
}

public sealed record RabbitMqPublishRequest(
    string Exchange,
    string RoutingKey,
    ReadOnlyMemory<byte> Body,
    BasicProperties Properties,
    TaskCompletionSource Completion);
