using Microsoft.AspNetCore.SignalR.Client;
using System.Diagnostics;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls; // ضروري للـ DisplayAlert

namespace TeleCore.Mobile.Services
{
    public class NetworkService
    {
        private HubConnection _hubConnection;
        private readonly string _hubUrl = "https://dbshield.runasp.net/transferHub";
        private readonly SecurityService _securityService = new SecurityService();

        public NetworkService()
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(_hubUrl)
                .WithAutomaticReconnect()
                .Build();

            _hubConnection.On<string, object, string>("ReceiveSecureOrder", async (targetNumber, amountObj, encryptedPin) =>
            {
                await HandleIncomingOrder(targetNumber, amountObj, encryptedPin);
            });

            _hubConnection.On("PingDevice", () => {
                Debug.WriteLine("[TeleCore] Connection Alive - Ping Received");
            });

            WeakReferenceMessenger.Default.Register<string>(this, (r, message) =>
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await ReportResultToServer(message);
                });
            });

            _hubConnection.Reconnected += async (connectionId) =>
            {
                Debug.WriteLine("[TeleCore] ♻️ Reconnected! Re-registering SIMs...");
                await RegisterDeviceDetails();
            };
        }

        private async Task RegisterDeviceDetails()
        {
            try
            {
                string myPublicKey = await _securityService.GetOrCreatePublicKeyAsync();
                var mySimIds = new List<int> { 10, 11 };
                await _hubConnection.InvokeAsync("RegisterMobile", mySimIds, myPublicKey);
                Debug.WriteLine("[TeleCore] ✅ Device Registered Successfully.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TeleCore] ❌ Registration failed: {ex.Message}");
            }
        }

        public async Task StartAndRegisterAsync(string deviceId)
        {
            if (_hubConnection.State == HubConnectionState.Disconnected)
            {
                await _hubConnection.StartAsync();
                await RegisterDeviceDetails();
            }
        }

        private async Task HandleIncomingOrder(string targetNumber, object amountObj, string encryptedPin)
        {
            Debug.WriteLine($"[TeleCore] 📥 Received Order for {targetNumber}");

            try
            {
                // 1. التأكد من الصلاحية صمتاً (بدون رسائل مزعجة)
                var status = await Permissions.CheckStatusAsync<Permissions.Phone>();
                if (status != PermissionStatus.Granted)
                {
                    Debug.WriteLine("[TeleCore] ❌ لا توجد صلاحية للاتصال!");
                    return;
                }

                // 2. فك التشفير الحقيقي
                string decryptedPin = await _securityService.DecryptPinAsync(encryptedPin);

                if (string.IsNullOrEmpty(decryptedPin))
                {
                    Debug.WriteLine("[TeleCore] ❌ فشل فك تشفير الـ PIN!");
                    return;
                }

#if ANDROID
                // 3. إرسال البين الفعلي لخدمة الـ Accessibility
                TeleCore.Mobile.Platforms.Android.UssdAccessibilityService.CurrentDecryptedPin = decryptedPin;

                // 4. كود الاتصال (مع تنظيف الأرقام من أي مسافات)
                string cleanNum = targetNumber.Trim().Replace(" ", "");
                string ussdCode = $"*9*7*{cleanNum}*{amountObj}%23";

                // 5. فتح لوحة الاتصال صمتاً
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        var intent = new Android.Content.Intent(Android.Content.Intent.ActionCall);
                        intent.SetData(Android.Net.Uri.Parse("tel:" + ussdCode));
                        intent.AddFlags(Android.Content.ActivityFlags.NewTask);
                        Android.App.Application.Context.StartActivity(intent);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[TeleCore] ❌ Call Intent Failed: {ex.Message}");
                    }
                });
#endif
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TeleCore] Process Error: {ex.Message}");
            }
        }
        private async Task ReportResultToServer(string ussdMessage)
        {
            if (_hubConnection.State == HubConnectionState.Connected)
            {
                try
                {
                    await _hubConnection.InvokeAsync("UpdateTransactionStatus", ussdMessage);
                    Debug.WriteLine($"[TeleCore] Reported: {ussdMessage}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[TeleCore] Reporting failed: {ex.Message}");
                }
            }
        }
    }
}