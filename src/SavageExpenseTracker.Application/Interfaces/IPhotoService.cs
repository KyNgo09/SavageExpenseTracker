using System.IO;
using System.Threading.Tasks;

namespace SavageExpenseTracker.Application.Interfaces
{
    public interface IPhotoService
    {
        Task<string> UploadPhotoAsync(Stream fileStream, string fileName, string folder);
        Task<bool> DeletePhotoAsync(string publicId);
    }
}