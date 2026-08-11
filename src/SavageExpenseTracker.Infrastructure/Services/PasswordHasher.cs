using System;
using System.Security.Cryptography;
using System.Text;
using BCrypt.Net;
using SavageExpenseTracker.Application.Interfaces;

namespace SavageExpenseTracker.Infrastructure.Services
{
    public class PasswordHasher : IPasswordHasher
    {
        public string HashPassword(string password)
        {
            var sha256Hash = PreHashSha256(password);
            return BCrypt.Net.BCrypt.HashPassword(sha256Hash);
        }

        public bool VerifyPassword(string password, string storedHash)
        {
            if (string.IsNullOrWhiteSpace(storedHash)) return false;

            var sha256Hash = PreHashSha256(password);

            if (storedHash.StartsWith("$2"))
            {
                return BCrypt.Net.BCrypt.Verify(sha256Hash, storedHash);
            }

            return storedHash.Equals(sha256Hash, StringComparison.OrdinalIgnoreCase);
        }

        private static string PreHashSha256(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            var builder = new StringBuilder();
            foreach (var b in bytes)
            {
                builder.Append(b.ToString("x2"));
            }
            return builder.ToString();
        }
    }
}
