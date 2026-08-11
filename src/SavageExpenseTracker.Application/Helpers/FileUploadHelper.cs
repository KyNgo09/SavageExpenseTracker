using System;
using System.IO;
using System.Linq;

namespace SavageExpenseTracker.Application.Helpers
{
    public static class FileUploadHelper
    {
        public static void ValidateImageFile(Stream stream, string fileName, string contentType, long fileLength, long maxSizeBytes, string[] allowedExtensions)
        {
            if (stream == null || fileLength == 0)
            {
                throw new InvalidOperationException("Please select an image file!");
            }

            if (fileLength > maxSizeBytes)
            {
                var maxMb = maxSizeBytes / (1024 * 1024);
                throw new InvalidOperationException($"File size must not exceed {maxMb} MB!");
            }

            var extension = Path.GetExtension(fileName)?.ToLowerInvariant();

            if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension) || !contentType.StartsWith("image/"))
            {
                var allowedList = string.Join(", ", allowedExtensions.Select(e => e.TrimStart('.').ToUpperInvariant()));
                throw new InvalidOperationException($"Invalid image file format! Only {allowedList} images are allowed.");
            }
        }
    }
}
