using System.ComponentModel.DataAnnotations;

namespace SavageExpenseTracker.Application.Dtos.User
{
    public class UpdateUserDto
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address format.")]
        [StringLength(255, ErrorMessage = "Email cannot exceed 255 characters.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Username is required.")]
        [StringLength(255, ErrorMessage = "Username cannot exceed 255 characters.")]
        public string UserName { get; set; } = string.Empty;

        [Range(0, double.MaxValue, ErrorMessage = "Hourly rate must be a non-negative value.")]
        public decimal HourlyRate { get; set; }
    }
}