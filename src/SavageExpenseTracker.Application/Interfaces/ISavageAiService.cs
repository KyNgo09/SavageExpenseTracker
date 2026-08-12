using System.Threading.Tasks;

namespace SavageExpenseTracker.Application.Interfaces
{
    public interface ISavageAiService
    {
        Task<string> GenerateSavageCommentAsync(string? description, decimal amount, decimal timeWork, string categoryName);
    }
}