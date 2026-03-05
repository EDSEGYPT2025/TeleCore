using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Maui.Devices;  // 👈 عشان DeviceInfo
using Microsoft.Maui.Storage; // 👈 عشان Preferences
using System.Net.Http;

namespace TeleCore.Mobile.Services // 👈 ده مهم جداً عشان باقي المشروع يشوفه
{
    public class NetworkService
    {
        private HubConnection _hubConnection;
        private readonly SecurityService _securityService;

        public NetworkService()
        {
            _securityService = new SecurityService();

            // إعداد الاتصال بالسيرفر الحقيقي
            string serverUrl = "https://dbshield.runasp.net/nodeHub";

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(serverUrl, options =>
                {
                    // 👇 ده "حل سحري" عشان موبايلات أندرويد 9 (زي الـ J7) متعملش بلوك للاتصال 
                    // بسبب شهادة الأمان بتاعت الاستضافة (SSL Certificate)
                    options.HttpMessageHandlerFactory = handler => new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                    };
                })
                .WithAutomaticReconnect()
                .Build();

            // استقبال الرد من السيرفر
            _hubConnection.On<string>("RegistrationResponse", (status) =>
            {
                // غيرنا Console لـ Debug عشان يظهرلك بوضوح في الـ Output بتاع الفيجوال ستوديو
                System.Diagnostics.Debug.WriteLine($"[TeleCore] Server Status: {status}");
            });
        }

        public async Task StartAndRegisterAsync()
        {
            try
            {
                // 1. توليد أو جلب المفتاح العام
                string publicKey = await _securityService.GetOrCreatePublicKeyAsync();

                // 2. جلب معرف فريد للجهاز
                string deviceId = Preferences.Default.Get("UniqueDeviceId", string.Empty);

                if (string.IsNullOrEmpty(deviceId))
                {
                    // توليد معرف جديد وحفظه في الجهاز للأبد
                    deviceId = Guid.NewGuid().ToString();
                    Preferences.Default.Set("UniqueDeviceId", deviceId);
                }
                string model = DeviceInfo.Current.Model;

                // 3. فتح الاتصال بالسيرفر
                if (_hubConnection.State == HubConnectionState.Disconnected)
                {
                    System.Diagnostics.Debug.WriteLine("[TeleCore] جاري الاتصال بالسيرفر...");
                    await _hubConnection.StartAsync();
                    System.Diagnostics.Debug.WriteLine("[TeleCore] تم الاتصال بنجاح!");
                }

                // 4. إرسال طلب التسجيل للسيرفر
                await _hubConnection.InvokeAsync("RegisterNode", deviceId, model, publicKey);
                System.Diagnostics.Debug.WriteLine("[TeleCore] تم إرسال بيانات الموبايل للداتابيز!");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TeleCore] خطأ في الاتصال: {ex.Message}");
            }
        }
    }
}