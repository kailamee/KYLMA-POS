using System;
using System.Security.Cryptography;
using System.Text;

namespace KYLMA.Data
{
    /// <summary>
    /// Password hashing that matches the schema in SQL.docx:
    /// PasswordHash = UPPERCASE_HEX(SHA-256(Salt + Password)), Salt = 32 hex characters.
    /// </summary>
    public static class PasswordHasher
    {
        public static string NewSalt()
        {
            var bytes = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            return BitConverter.ToString(bytes).Replace("-", "");
        }

        public static string Hash(string salt, string password)
        {
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(salt + password));
                return BitConverter.ToString(hash).Replace("-", "");
            }
        }

        public static bool Verify(string password, string salt, string storedHash)
        {
            if (password == null || salt == null || storedHash == null) return false;
            var computed = Hash(salt, password);
            return FixedTimeEquals(computed, storedHash.Trim().ToUpperInvariant());
        }

        private static bool FixedTimeEquals(string a, string b)
        {
            if (a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}
