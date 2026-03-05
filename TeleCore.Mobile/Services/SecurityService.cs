using System.Security.Cryptography;

namespace TeleCore.Mobile.Services
{
    public class SecurityService
    {
        private const string PrivateKeyAlias = "Mobile_Private_Key";

        public async Task<string> GetOrCreatePublicKeyAsync()
        {
            // 1. التأكد إذا كان هناك مفتاح خاص مخزن مسبقاً
            var existingPrivateKey = await SecureStorage.Default.GetAsync(PrivateKeyAlias);

            using var rsa = new RSACryptoServiceProvider(2048);

            if (string.IsNullOrEmpty(existingPrivateKey))
            {
                // 2. توليد مفاتيح جديدة لأول مرة
                // المفتاح الخاص (Private) - يتم تحويله لـ XML وتخزينه مشفراً في النظام
                var privateKeyXml = rsa.ToXmlString(true);
                await SecureStorage.Default.SetAsync(PrivateKeyAlias, privateKeyXml);
            }
            else
            {
                // 3. تحميل المفتاح الخاص الموجود
                rsa.FromXmlString(existingPrivateKey);
            }

            // 4. استخراج المفتاح العام (Public Key) لإرساله للسيرفر
            return rsa.ToXmlString(false);
        }

        public async Task<string> DecryptPinAsync(string encryptedPinBase64)
        {
            var privateKeyXml = await SecureStorage.Default.GetAsync(PrivateKeyAlias);
            if (string.IsNullOrEmpty(privateKeyXml)) throw new Exception("Security keys not initialized.");

            using var rsa = new RSACryptoServiceProvider(2048);
            rsa.FromXmlString(privateKeyXml);

            var encryptedData = Convert.FromBase64String(encryptedPinBase64);
            var decryptedData = rsa.Decrypt(encryptedData, false);

            return System.Text.Encoding.UTF8.GetString(decryptedData);
        }
    }
}