using Microsoft.Extensions.Logging;
using TeleCore.Mobile.Pages;
using TeleCore.Mobile.Services;

namespace TeleCore.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // 1️⃣ تسجيل الخدمات (Services)
#if ANDROID
        // تسجيل خدمة الـ USSD (لازم تكون Transient أو Singleton)
        builder.Services.AddSingleton<IUssdService, TeleCore.Mobile.Platforms.Android.AndroidUssdService>();

        // تسجيل خدمة الـ API كـ Singleton لضمان كفاءة الـ Network Requests
        builder.Services.AddSingleton<ApiService>();
#endif

        // 2️⃣ تسجيل الصفحات (Pages)
        // بنسجل الـ AppShell والـ MainPage كـ Singleton لأنهم بيفضلوا عايشين طول مدة استخدام التطبيق
        builder.Services.AddSingleton<AppShell>();
        builder.Services.AddSingleton<MainPage>();

        // بنسجل الـ SplashPage والـ HistoryPage كـ Transient عشان يتم إنشائهم عند الحاجة فقط
        builder.Services.AddTransient<SplashPage>();
        builder.Services.AddTransient<HistoryPage>();

        return builder.Build();
    }
}