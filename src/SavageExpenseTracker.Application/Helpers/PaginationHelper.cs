namespace SavageExpenseTracker.Application.Helpers
{
    public static class PaginationHelper
    {
        public static (int pageNumber, int pageSize) Normalize(int pageNumber, int pageSize, int defaultPageSize = 10, int maxPageSize = 100)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = defaultPageSize;
            if (pageSize > maxPageSize) pageSize = maxPageSize;

            return (pageNumber, pageSize);
        }
    }
}
