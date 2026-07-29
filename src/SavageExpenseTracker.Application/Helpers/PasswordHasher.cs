using System;
using System.Security.Cryptography;
using System.Text;
using BCrypt.Net;

namespace SavageExpenseTracker.Application.Helpers
{
    public static class PasswordHasher
    {
        /// <summary>
        /// Băm mật khẩu theo công thức BCrypt(SHA256(Password)).
        /// SHA-256 đưa mật khẩu về chuỗi 64 ký tự cố định, loại bỏ giới hạn 72-byte của BCrypt và phòng chống tấn công DoS.
        /// BCrypt thêm Salt ngẫu nhiên và key-stretching phòng chống Rainbow Table.
        /// </summary>
        public static string HashPassword(string password)
        {
            var sha256Hash = PreHashSha256(password);
            return BCrypt.Net.BCrypt.HashPassword(sha256Hash);
        }

        /// <summary>
        /// Kiểm tra mật khẩu nhập vào có khớp với hash trong CSDL hay không.
        /// </summary>
        public static bool VerifyPassword(string password, string storedHash)
        {
            if (string.IsNullOrWhiteSpace(storedHash)) return false;

            var sha256Hash = PreHashSha256(password);

            // Kiểm tra xem storedHash có đúng định dạng BCrypt hash (chuỗi bắt đầu bằng $2) hay không
            if (storedHash.StartsWith("$2"))
            {
                return BCrypt.Net.BCrypt.Verify(sha256Hash, storedHash);
            }

            // Hỗ trợ tương thích ngược cho hash SHA-256 cũ (nếu có dữ liệu test cũ)
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
