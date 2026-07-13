namespace SavageExpenseTracker.Application.Dtos.Challenge
{
    public class CreateChallengeDto
    {
        public string Name { get; set; } = string.Empty;
        public DateTime DateStart { get; set; }
        public DateTime DateEnd { get; set; }
    }
}