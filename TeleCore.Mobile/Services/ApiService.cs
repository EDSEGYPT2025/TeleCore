using System.Text;
using System.Text.Json; // 👈 استخدمنا مكتبة دوت نت الأصلية

namespace TeleCore.Mobile.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;

        // الرابط الأونلاين الجديد للسيرفر
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
                    var error = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"[TeleCore API] Error: {error}");
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

                System.Diagnostics.Debug.WriteLine($"[TeleCore] 🔄 جاري مزامنة {pendingTx.Count} عملية معلقة...");

                foreach (var tx in pendingTx)
                {
                    int simId = 1;
                    string url = $"{BaseUrl}/transactions/send-money?simId={simId}&targetNumber={tx.ReceiverNumber}&amount={tx.Amount}";

                    var response = await _httpClient.PostAsync(url, null);

                    if (response.IsSuccessStatusCode)
                    {
                        await db.MarkAsSyncedAsync(tx.Id);
                        System.Diagnostics.Debug.WriteLine($"[TeleCore] ✅ تم مزامنة العملية {tx.TransactionId}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TeleCore] ⚠️ فشل المزامنة: {ex.Message}");
            }
        }

        // 🛑 التعديل الأهم هنا: ربطنا الدالة بالـ BaseUrl
        public async Task<List<int>> GetMySimsFromServer(string deviceId)
        {
            try
            {
                // تم التعديل لاستخدام الرابط الأونلاين
                string url = $"{BaseUrl}/Sims/my-assigned-sims/{deviceId}";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();

                    // استخدام System.Text.Json بدلاً من Newtonsoft
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    return JsonSerializer.Deserialize<List<int>>(content, options) ?? new List<int>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching sims: {ex.Message}");
            }
            return new List<int>();
        }



        // داخل كلاس ApiService.cs ضيف الدالة دي:
        public async Task<bool> SyncSingleTransactionAsync(Models.TransactionRecord tx)
        {
            try
            {
                string url = $"{BaseUrl}/transactions/sync";

                // تحويل الريكورد لـ JSON
                var json = JsonSerializer.Serialize(tx);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"[TeleCore] ✅ تمت مزامنة العملية {tx.TransactionId} مع السيرفر.");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TeleCore] ❌ فشل المزامنة: {ex.Message}");
                return false;
            }
        }
    }
}