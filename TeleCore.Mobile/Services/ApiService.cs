using System.Text;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Net.Http;

namespace TeleCore.Mobile.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;

        // الرابط الأونلاين الموحد للسيرفر
        private const string BaseUrl = "https://dbshield.runasp.net/api";

        public ApiService()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(15);
        }

        public async Task<bool> ConfirmTransactionOnServerAsync(string targetNumber, double amount)
        {
            try
            {
                int simId = 1;
                string url = $"{BaseUrl}/transactions/send-money?simId={simId}&targetNumber={targetNumber}&amount={amount}";

                var response = await _httpClient.PostAsync(url, null);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"[TeleCore API] Success: {content}");
                    return true;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[TeleCore API] Error: {response.StatusCode}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TeleCore API] Exception: {ex.Message}");
                return false;
            }
        }

        public async Task SyncPendingTransactionsAsync()
        {
            try
            {
                var db = new DatabaseService();
                var pendingTx = await db.GetUnsyncedTransactionsAsync();

                if (pendingTx.Count == 0) return;

                System.Diagnostics.Debug.WriteLine($"[TeleCore] 🔄 جاري مزامنة {pendingTx.Count} عملية...");

                foreach (var tx in pendingTx)
                {
                    int simId = 1;
                    string url = $"{BaseUrl}/transactions/send-money?simId={simId}&targetNumber={tx.ReceiverNumber}&amount={tx.Amount}";
                    var response = await _httpClient.PostAsync(url, null);

                    if (response.IsSuccessStatusCode)
                    {
                        await db.MarkAsSyncedAsync(tx.Id);
                        System.Diagnostics.Debug.WriteLine($"[TeleCore] ✅ تم مزامنة {tx.TransactionId}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TeleCore] ⚠️ فشل المزامنة: {ex.Message}");
            }
        }

        public async Task<List<int>> GetMySimsFromServer(string deviceId)
        {
            try
            {
                string url = $"{BaseUrl}/Sims/my-assigned-sims/{deviceId}";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    return JsonSerializer.Deserialize<List<int>>(content, options) ?? new List<int>();
                }
                System.Diagnostics.Debug.WriteLine($"[TeleCore API] GetSims Error: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TeleCore API] Error: {ex.Message}");
            }
            return new List<int>();
        }

        public async Task<bool> SyncSingleTransactionAsync(Models.TransactionRecord tx)
        {
            try
            {
                string url = $"{BaseUrl}/transactions/sync";
                var json = JsonSerializer.Serialize(tx);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TeleCore] SyncSingle Ex: {ex.Message}");
                return false;
            }
        }

        // 1. كلاسات الرسائل المحدثة
        public class DeviceSyncRequest
        {
            public string DeviceUniqueId { get; set; } = string.Empty;
            public string DeviceModel { get; set; } = string.Empty;
            public string PublicKey { get; set; } = string.Empty; // 👈 تم الإضافة لحل مشكلة N/A
        }

        public class DeviceSyncResponse
        {
            public bool Success { get; set; }
            public bool IsAuthorized { get; set; }
            public string Message { get; set; } = string.Empty;
            public Dictionary<int, string> AssignedSims { get; set; } = new Dictionary<int, string>();
        }

        // 2. دالة "النبض التلقائي" المحدثة
        public async Task<DeviceSyncResponse> SyncDeviceWithRadarAsync()
        {
            try
            {
                // 🛡️ توليد وحفظ مفتاح فريد للجهاز إذا لم يكن موجوداً
                string myKey = Microsoft.Maui.Storage.Preferences.Default.Get("DevicePublicKey", "");
                if (string.IsNullOrEmpty(myKey))
                {
                    myKey = "TC-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
                    Microsoft.Maui.Storage.Preferences.Default.Set("DevicePublicKey", myKey);
                }

                var request = new DeviceSyncRequest
                {
                    DeviceUniqueId = DeviceInfo.Current.Name,
                    DeviceModel = $"{DeviceInfo.Current.Manufacturer} {DeviceInfo.Current.Model}",
                    PublicKey = myKey // 👈 إرسال المفتاح للسيرفر
                };

                string url = $"{BaseUrl}/Device/Sync";
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    return JsonSerializer.Deserialize<DeviceSyncResponse>(responseString, options);
                }

                System.Diagnostics.Debug.WriteLine($"[Radar Sync] Server responded with: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Radar Sync] Critical Exception: {ex.Message}");
            }

            return new DeviceSyncResponse { Success = false, Message = "فشل الاتصال بالرادار" };
        }
    }
}