using Microsoft.AspNetCore.SignalR.Client;
using TeleCore.Mobile.Services;

public class NetworkService
{
    private HubConnection _hubConnection;
    private readonly SecurityService _securityService;

    public NetworkService()
    {
        _securityService = new SecurityService();

        // إعداد الاتصال بالسيرفر
        // الرابط الكامل للـ Hub على السيرفر الحقيقي
        string serverUrl = "https://dbshield.runasp.net/nodeHub";

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(serverUrl)
            .WithAutomaticReconnect()
            .Build();

        // استقبال الرد من السيرفر
        _hubConnection.On<string>("RegistrationResponse", (status) =>
        {
            Console.WriteLine($"Server Status: {status}");
        });
    }

    public async Task StartAndRegisterAsync()
    {
        try
        {
            // 1. توليد أو جلب المفتاح العام (كودك أنت)
            string publicKey = await _securityService.GetOrCreatePublicKeyAsync();

            // 2. جلب بيانات الموبايل
            // جلب معرف فريد للجهاز - سيعمل على أندرويد وويندوز بشكل ثابت
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
                await _hubConnection.StartAsync();
            }

            // 4. إرسال طلب التسجيل للسيرفر
            await _hubConnection.InvokeAsync("RegisterNode", deviceId, model, publicKey);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}