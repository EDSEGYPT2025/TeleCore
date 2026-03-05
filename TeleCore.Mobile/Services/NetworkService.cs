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
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    if (Application.Current?.MainPage != null)
                    {
                        await Application.Current.MainPage.DisplayAlert(
                            "📥 طلب جديد",
                            $"استلمت طلب تحويل للرقم: {targetNumber}\nبمبلغ: {amountObj}",
                            "تنفيذ");
                    }
                });

                var status = await Permissions.CheckStatusAsync<Permissions.Phone>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.Phone>();
                    if (status != PermissionStatus.Granted) return;
                }

                // 🔐 التعديل هنا: فك التشفير الحقيقي للـ PIN
                string decryptedPin = await _securityService.DecryptPinAsync(encryptedPin);

                // حماية: لو فك التشفير فشل أو البين كان فاضي، نوقف العملية
                if (string.IsNullOrEmpty(decryptedPin) || decryptedPin.Length > 8)
                {
                    Debug.WriteLine("[TeleCore] ❌ فشل فك تشفير الـ PIN أو التنسيق خاطئ!");
                    return;
                }

                Debug.WriteLine($"[TeleCore] ✅ تم فك تشفير الـ PIN بنجاح");

#if ANDROID
                // إرسال البين الفعلي لخدمة الوصول
                TeleCore.Mobile.Platforms.Android.UssdAccessibilityService.CurrentDecryptedPin = decryptedPin;

                // كود الاتصال
                string ussdCode = $"*9*7*{targetNumber}*{amountObj}%23";

                await Task.Delay(500);

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