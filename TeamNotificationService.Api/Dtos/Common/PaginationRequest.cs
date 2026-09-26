using TeamNotificationService.Domain.Exceptions;

namespace TeamNotificationService.Api.Dtos.Common;

public class PaginationRequest
{
    private int currentPage = 1;
    private int itemsPerPage = int.MaxValue;

    public int CurrentPage
    {
        get => currentPage;
        init
        {
            if (value < 1)
                throw new UnprocessableEntityException(
                    "Current page must be a positive integer.",
                    new Dictionary<string, object?>
                    {
                        ["field"] = "currentPage"
                    }
                );

            currentPage = value;
        }
    }

    public int ItemsPerPage
    {
        get => itemsPerPage;
        init
        {
            if (value < 1)
                throw new UnprocessableEntityException(
                    "Items per page must be a positive integer.",
                    new Dictionary<string, object?>
                    {
                        ["field"] = "itemsPerPage"
                    }
                );

            itemsPerPage = value;
        }
    }

}
