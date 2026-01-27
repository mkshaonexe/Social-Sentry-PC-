using System;
using System.Security.Cryptography;
using System.Text;

namespace Social_Sentry.Services
{
    public class SecurityService
    {
        private const int SaltSize = 16; // 128 bit
        private const int KeySize = 32; // 256 bit
        private const int Iterations = 100000; // PBKDF2 iterations

        public static (string Hash, string Salt) HashPin(string pin)
        {
            using (var algorithm = new Rfc2898DeriveBytes(pin, SaltSize, Iterations, HashAlgorithmName.SHA256))
            {
                string key = Convert.ToBase64String(algorithm.GetBytes(KeySize));
                string salt = Convert.ToBase64String(algorithm.Salt);

                return (key, salt);
            }
        }

        public static bool VerifyPin(string pin, string storedHash, string storedSalt)
        {
            byte[] salt = Convert.FromBase64String(storedSalt);
            
            using (var algorithm = new Rfc2898DeriveBytes(pin, salt, Iterations, HashAlgorithmName.SHA256))
            {
                byte[] keyToCheck = algorithm.GetBytes(KeySize);
                string keyToCheckStr = Convert.ToBase64String(keyToCheck);

                return keyToCheckStr == storedHash;
            }
        }
    }
}
