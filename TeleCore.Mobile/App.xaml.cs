using Microsoft.Extensions.DependencyInjection;
using TeleCore.Mobile.Pages; // تأكد إن ده مسار مجلد Pages عندك

namespace TeleCore.Mobile;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        // 🗑️ تم مسح كود الـ SignalR من هنا عشان هننقله لصفحة الـ Splash
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // 🚀 التطبيق هيفتح على شاشة الـ Splash كواجهة مبدئية
        return new Window(new SplashPage());
    }
}