using System.Security.Claims;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Moq;
using TeamNotificationService.Domain.Exceptions;
using TeamNotificationService.Infrastructure.Contexts;
using TeamNotificationService.Infrastructure.Options;
using TeamNotificationService.Infrastructure.Services.Email;
using Xunit;

namespace TeamNotificationService.Infrastructure.Tests.Services.Email;

public class HandlebarsEmailBinderTests
{
    [Fact]
    public void BindTemplate_ShouldRenderNestedDataAndEscapeHtml_WhenDataIsProvided()
    {
        using var data = JsonDocument.Parse("{\"user\":{\"name\":\"<Alice>\"},\"items\":[\"one\",\"two\"]}");
        var rendered = HandlebarsEmailBinder.BindTemplate("Hello {{user.name}} {{#each items}}{{this}} {{/each}}", data);
        rendered.Should().Be("Hello &lt;Alice&gt; one two ");
    }

    [Fact]
    public void BindTemplate_ShouldThrowInvalidEmailTemplate_WhenSyntaxIsInvalid()
    {
        var action = () => HandlebarsEmailBinder.BindTemplate("{{#if user}}", null);
        action.Should().Throw<InvalidEmailTemplateException>();
    }
}
