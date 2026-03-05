using System.Security.Cryptography;
using Microsoft.Maui.Storage;

namespace TeleCore.Mobile.Services
{
    public class SecurityService
    {
        private const string PrivateKeyAlias = "Mobile_Private_Key";

        public async Task<string> GetOrCreatePublicKeyAsync()
        {
            var existingPrivateKey = await SecureStorage.Default.GetAsync(PrivateKeyAlias);
            using var rsa = RSA.Create(2048);

            if (string.IsNullOrEmpty(existingPrivateKey))
            {
                // توليد وتخزين المفتاح الخاص بأسلوب XML (متوافق مع كودك)
                var privateKeyXml = rsa.ToXmlString(true);
                await SecureStorage.Default.SetAsync(PrivateKeyAlias, privateKeyXml);
            }
            else
            {
                rsa.FromXmlString(existingPrivateKey);
            }

            return rsa.ToXmlString(false); // إرجاع المفتاح العام للسيرفر
        }

        public async Task<string> DecryptPinAsync(string encryptedPinBase64)
        {
            try
            {
                var privateKeyXml = await SecureStorage.Default.GetAsync(PrivateKeyAlias);
                if (string.IsNullOrEmpty(privateKeyXml)) throw new Exception("Keys not found.");

                using var rsa = RSA.Create();
                rsa.FromXmlString(privateKeyXml);

                var encryptedData = Convert.FromBase64String(encryptedPinBase64);
                // ⚠️ ملاحظة: يجب أن يتطابق التشفير في السيرفر مع OAEPSHA256 أو Pkcs1
                var decryptedData = rsa.Decrypt(encryptedData, RSAEncryptionPadding.Pkcs1);

                return System.Text.Encoding.UTF8.GetString(decryptedData);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Security] Decryption Failed: {ex.Message}");
                return null;
            }
        }
    }
}