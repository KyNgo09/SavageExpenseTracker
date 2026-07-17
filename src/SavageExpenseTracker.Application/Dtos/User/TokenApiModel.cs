namespace SavageExpenseTracker.Application.Dtos.User
{
    public class TokenApiModel
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }
}