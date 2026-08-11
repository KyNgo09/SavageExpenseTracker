using System;
using System.ComponentModel.DataAnnotations;

namespace SavageExpenseTracker.Application.Dtos.Challenge
{
    public class CreateChallengeDto
    {
        [Required(ErrorMessage = "Challenge name is required.")]
        [StringLength(255, ErrorMessage = "Challenge name cannot exceed 255 characters.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Start date is required.")]
        public DateTime DateStart { get; set; }

        [Required(ErrorMessage = "End date is required.")]
        public DateTime DateEnd { get; set; }
    }
}