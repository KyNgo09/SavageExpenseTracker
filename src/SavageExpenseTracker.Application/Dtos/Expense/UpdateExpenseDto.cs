namespace SavageExpenseTracker.Application.Dtos.Expense
{
    public class UpdateExpenseDto
    {
        public string? Description { get; set; }
        public decimal Amount { get; set; }
    }
}