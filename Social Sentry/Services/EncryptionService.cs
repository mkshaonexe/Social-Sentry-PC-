using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Social_Sentry.Services
{
    public class EncryptionService
    {
        private readonly string _keyPath;
        private byte[] _aesKey;
        private const int KEY_SIZE = 32; // 256 bits

        public EncryptionService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var folder = Path.Combine(appDataPath, "SocialSentry");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            
            _keyPath = Path.Combine(folder, "master.key");
            LoadOrGenerateKey();
        }

        private void LoadOrGenerateKey()
        {
            try
            {
                if (File.Exists(_keyPath))
                {
                    // Read encrypted key
                    byte[] encryptedKey = File.ReadAllBytes(_keyPath);
                    // Decrypt using DPAPI (CurrentUser)
                    _aesKey = ProtectedData.Unprotect(encryptedKey, null, DataProtectionScope.CurrentUser);
                }
                else
                {
                    // Generate new random key
                    _aesKey = RandomNumberGenerator.GetBytes(KEY_SIZE);
                    // Encrypt using DPAPI
                    byte[] encryptedKey = ProtectedData.Protect(_aesKey, null, DataProtectionScope.CurrentUser);
                    File.WriteAllBytes(_keyPath, encryptedKey);
                }
            }
            catch (Exception)
            {
                // Fallback / Reset if decryption fails (e.g. machine changed)
                // In production, might want better recovery, but for now reset security
                _aesKey = RandomNumberGenerator.GetBytes(KEY_SIZE);
                byte[] encryptedKey = ProtectedData.Protect(_aesKey, null, DataProtectionScope.CurrentUser);
                File.WriteAllBytes(_keyPath, encryptedKey);
            }
        }

        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return plainText;

            using (Aes aes = Aes.Create())
            {
                aes.Key = _aesKey;
                aes.GenerateIV();
                
                using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                using (var ms = new MemoryStream())
                {
                    // Prepend IV to the stream
                    ms.Write(aes.IV, 0, aes.IV.Length);
                    
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    using (var sw = new StreamWriter(cs))
                    {
                        sw.Write(plainText);
                    }
                    
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        public string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return cipherText;

            try
            {
                byte[] fullCipher = Convert.FromBase64String(cipherText);
                
                using (Aes aes = Aes.Create())
                {
                    aes.Key = _aesKey;
                    
                    // Extract IV (first 16 bytes for AES block size)
                    byte[] iv = new byte[aes.BlockSize / 8];
                    Array.Copy(fullCipher, 0, iv, 0, iv.Length);
                    aes.IV = iv;

                    using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                    using (var ms = new MemoryStream(fullCipher, iv.Length, fullCipher.Length - iv.Length))
                    using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    using (var sr = new StreamReader(cs))
                    {
                        return sr.ReadToEnd();
                    }
                }
            }
            catch
            {
                // If decryption fails (e.g. old plain data), return generic or original
                // Returns string so we don't crash UI
                return "[Encrypted Data]";
            }
        }
    }
}
