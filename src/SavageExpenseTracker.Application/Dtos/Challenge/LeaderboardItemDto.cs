namespace SavageExpenseTracker.Application.Dtos.Challenge
{
    public class LeaderboardItemDto
    {
        public Guid UserId { get; set; }
        public decimal TotalTimeWork { get; set; }
        public decimal TotalAmount { get; set; }
        public string Title { get; set; } = string.Empty;
    }
}