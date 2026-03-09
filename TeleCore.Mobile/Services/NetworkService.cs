using Microsoft.AspNetCore.SignalR.Client;
using System.Diagnostics;
using CommunityToolkit.Mvvm.Messaging;
using System.Linq;
using CommunityToolkit.Mvvm.Messaging.Messages;
using System.Globalization;

namespace TeleCore.Mobile.Services
{
    public class NetworkService
    {
        private HubConnection _hubConnection;
        private readonly string _hubUrl = "https://dbshield.runasp.net/TransactionHub";

        private static NetworkService _instance;
        public static NetworkService Instance => _instance ??= new NetworkService();

        public bool IsConnected => _hubConnection != null && _hubConnection.State == HubConnectionState.Connected;

        private NetworkService()
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(_hubUrl)
                .WithAutomaticReconnect()
                .Build();

            // 🎯 استقبال "النص الخام" من السيرفر (تكتيك القصف الشامل)
            _hubConnection.On<string>("ReceiveRawOrder", (rawPayload) =>
            {
                Debug.WriteLine($"[NetworkService] 📥 BINGO! Raw Signal Received: {rawPayload}");

                try
                {
                    // فك النص (مثال: "1,01012345678,50.5,99")
                    var parts = rawPayload.Split(',');
                    if (parts.Length == 4)
                    {
                        int simId = int.Parse(parts[0]);
                        string target = parts[1];
                        double amount = double.Parse(parts[2], CultureInfo.InvariantCulture);
                        int txId = int.Parse(parts[3]);

                        // 🛡️ هل هذا الأمر يخص هذا الموبايل؟
                        string savedSims = Preferences.Default.Get("MySimIds", "");
                        var mySims = savedSims.Split(',').Where(x => int.TryParse(x, out _)).Select(int.Parse).ToList();

                        if (mySims.Contains(simId))
                        {
                            Debug.WriteLine($"[NetworkService] 🚀 إشارة مطابقة! جاري الإرسال لواجهة التطبيق...");
                            var order = new RemoteOrder { SimId = simId, TargetNumber = target, Amount = amount };
                            WeakReferenceMessenger.Default.Send(new RemoteOrderMessage(order));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[NetworkService] ❌ Error Processing Signal: {ex.Message}");
                }
            });

            _hubConnection.On("PingDevice", () => Debug.WriteLine("[NetworkService] Connection Alive"));
        }

        public async Task StartAsync()
        {
            if (_hubConnection.State == HubConnectionState.Disconnected)
            {
                try
                {
                    await _hubConnection.StartAsync();
                    string savedSims = Preferences.Default.Get("MySimIds", "");
                    var mySims = savedSims.Split(',').Where(x => int.TryParse(x, out _)).Select(int.Parse).ToList();
                    if (mySims.Any()) await RegisterDeviceDetails(mySims);
                }
                catch { Debug.WriteLine("[NetworkService] ❌ Start Failed"); }
            }
        }

        private async Task RegisterDeviceDetails(List<int> mySimIds)
        {
            try { await _hubConnection.InvokeAsync("RegisterMobile", mySimIds); }
            catch { Debug.WriteLine("[NetworkService] ❌ Registration Failed"); }
        }

        public async Task SendResultToServerAsync(string result)
        {
            if (IsConnected) await _hubConnection.InvokeAsync("UpdateTransactionStatus", result);
        }

        public async Task SaveSimsAndReconnectAsync(List<int> simIds)
        {
            string idsString = string.Join(",", simIds);
            if (idsString == Preferences.Default.Get("MySimIds", "") && IsConnected) return;
            Preferences.Default.Set("MySimIds", idsString);
            if (IsConnected) await _hubConnection.StopAsync();
            await StartAsync();
        }

        public class RemoteOrder { public int SimId { get; set; } public string TargetNumber { get; set; } = ""; public double Amount { get; set; } }
        public class RemoteOrderMessage : ValueChangedMessage<RemoteOrder> { public RemoteOrderMessage(RemoteOrder value) : base(value) { } }
    }
}