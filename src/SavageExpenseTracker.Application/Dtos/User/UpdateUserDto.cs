namespace SavageExpenseTracker.Application.Dtos.User
{
    public class UpdateUserDto
    {
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public decimal HourlyRate { get; set; }
    }
}