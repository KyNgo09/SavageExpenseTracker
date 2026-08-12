namespace SavageExpenseTracker.Infrastructure.Options
{
    public class AiOptions
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string ApiUrl { get; set; } = string.Empty;
        public string FallbackModels { get; set; } = string.Empty;
    }
}
