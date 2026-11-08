using AwesomeAssertions;
using TeamNotificationService.Domain.Constants;
using TeamNotificationService.Domain.Enums.NotificationDeliveries;
using TeamNotificationService.Domain.Exceptions;
using TeamNotificationService.Domain.Utils;
using Xunit;

namespace TeamNotificationService.Domain.Tests.Utils;

public class Sha256UtilsTests
{
    [Fact]
    public void ComputeHexHash_ShouldReturnSha256_WhenInputIsProvided() =>
        Sha256Utils.ComputeHexHash("abc").Should().Be("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad");
}
