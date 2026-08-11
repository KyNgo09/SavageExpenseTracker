using System.ComponentModel.DataAnnotations;

namespace SavageExpenseTracker.Application.Dtos.User
{
    public class TokenApiModel
    {
        [Required(ErrorMessage = "AccessToken is required.")]
        public string AccessToken { get; set; } = string.Empty;

        [Required(ErrorMessage = "RefreshToken is required.")]
        public string RefreshToken { get; set; } = string.Empty;
    }
}