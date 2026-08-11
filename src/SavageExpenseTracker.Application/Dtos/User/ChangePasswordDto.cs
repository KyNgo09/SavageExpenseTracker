using System.ComponentModel.DataAnnotations;

namespace SavageExpenseTracker.Application.Dtos.User
{
    public class ChangePasswordDto
    {
        [Required(ErrorMessage = "Old password is required.")]
        public string OldPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required.")]
        [MinLength(6, ErrorMessage = "New password must be at least 6 characters long.")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm new password is required.")]
        [Compare(nameof(NewPassword), ErrorMessage = "New password and confirm new password do not match.")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }
}