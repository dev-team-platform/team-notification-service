using TeamNotificationService.Application.Models.Notifications;

namespace TeamNotificationService.Api.Dtos.V1.Notifications;

public sealed class GetLatelyNotificationForUserRequest
{
    public static GetLatelyNotificationForUserRequestModel ToModel(
        GetLatelyNotificationForUserRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new GetLatelyNotificationForUserRequestModel();
    }
}
