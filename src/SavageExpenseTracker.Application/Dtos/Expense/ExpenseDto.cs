namespace SavageExpenseTracker.Application.Dtos.Expense
{
    public class ExpenseDto
    {
        public long Id { get; set; }
        public Guid UserId { get; set; }
        public long CategoryId { get; set; }
        public string? ImageUrl { get; set; }
        public string? Description { get; set; }
        public decimal Amount { get; set; }
        public decimal TimeWork { get; set; }
        public string? SavageComment { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}