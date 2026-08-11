using System;
using System.IO;
using System.Threading.Tasks;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Configuration;
using SavageExpenseTracker.Application.Interfaces;

namespace SavageExpenseTracker.Infrastructure.Services
{
    public class CloudinaryPhotoService : IPhotoService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryPhotoService(IConfiguration config)
        {
            var acc = new Account(
                config["Cloudinary:CloudName"],
                config["Cloudinary:ApiKey"],
                config["Cloudinary:ApiSecret"]
            );

            _cloudinary = new Cloudinary(acc);
        }

        public async Task<string> UploadPhotoAsync(Stream fileStream, string fileName, string folder)
        {
            if (fileStream == null || fileStream.Length == 0)
                throw new ArgumentException("File is invalid");

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(fileName, fileStream),
                Folder = folder,
                Transformation = new Transformation().Width(1000).Crop("limit").Quality("auto").FetchFormat("auto")
            };    

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);
            if (uploadResult.Error != null)
                throw new Exception(uploadResult.Error.Message);

            return uploadResult.SecureUrl.AbsoluteUri;    
        }

        public async Task<bool> DeletePhotoAsync(string publicId)
        {
            var deleteParams = new DeletionParams(publicId);
            var result = await _cloudinary.DestroyAsync(deleteParams);
            return result.Result == "ok";  
        }
    }
}