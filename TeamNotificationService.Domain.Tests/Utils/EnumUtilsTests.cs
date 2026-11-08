using AwesomeAssertions;
using TeamNotificationService.Domain.Constants;
using TeamNotificationService.Domain.Enums.NotificationDeliveries;
using TeamNotificationService.Domain.Exceptions;
using TeamNotificationService.Domain.Utils;
using Xunit;

namespace TeamNotificationService.Domain.Tests.Utils;

public class EnumUtilsTests
{
    [Theory]
    [InlineData("email", ChannelType.Email)]
    [InlineData("BELL", ChannelType.Bell)]
    public void Parse_ShouldParseEnum_WhenNameUsesDifferentCasing(string name, ChannelType expected) =>
        EnumUtils.Parse<ChannelType>(name).Should().Be(expected);

    [Fact]
    public void Parse_ShouldThrowUnprocessableEntity_WhenEnumNameIsInvalid()
    {
        var action = () => EnumUtils.Parse<ChannelType>("sms");
        action.Should().Throw<UnprocessableEntityException>().Which.Details!["value"].Should().Be("sms");
    }

    [Fact]
    public void ToString_ShouldThrowUnprocessableEntity_WhenEnumValueIsUndefined()
    {
        var action = () => EnumUtils.ToString((ChannelType)999);
        action.Should().Throw<UnprocessableEntityException>();
    }
}
