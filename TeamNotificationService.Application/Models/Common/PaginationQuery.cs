using TeamNotificationService.Domain.Exceptions;

namespace TeamNotificationService.Application.Models.Common;

public class PaginationQuery
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
                        ["field"] = "currentPage",
                        ["currentPage"] = value
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
                        ["field"] = "itemsPerPage",
                        ["itemsPerPage"] = value
                    }
                );

            itemsPerPage = value;
        }
    }

}
