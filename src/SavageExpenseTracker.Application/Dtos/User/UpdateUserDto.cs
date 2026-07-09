namespace SavageExpenseTracker.Application.Dtos.User
{
    public class UpdateUserDto
    {
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public decimal HourlyRate { get; set; }
    }
}