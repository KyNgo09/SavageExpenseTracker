namespace SavageExpenseTracker.Application.Dtos.User
{
    public class CreateUserDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public decimal HourlyRate { get; set; } = 20000;
    }
}