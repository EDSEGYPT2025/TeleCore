using Microsoft.AspNetCore.SignalR.Client;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq; // ضرورية عشان نقدر نحول النص لأرقام

namespace TeleCore.Mobile.Services
{
    public class SignalRService
    {
        private HubConnection hubConnection;

        private static SignalRService _instance;
        public static SignalRService Instance => _instance ??= new SignalRService();

        public bool IsConnected => hubConnection != null && hubConnection.State == HubConnectionState.Connected;

        private SignalRService()
        {
            // بناء الاتصال
            hubConnection = new HubConnectionBuilder()
                .WithUrl("https://dbshield.runasp.net/TransactionHub")
                .WithAutomaticReconnect()
                .Build();

            // استقبال أمر التحويل
            hubConnection.On<RemoteOrder>("ReceiveOrder", (order) =>
            {
                WeakReferenceMessenger.Default.Send(new RemoteOrderMessage(order));
            });

            // استقبال أمر الـ Ping من لوحة التحكم للبحث عن الموبايل
            hubConnection.On("PingDevice", () =>
            {
                WeakReferenceMessenger.Default.Send("PingReceived", "PingDevice");
            });
        }

        public async Task ConnectAsync()
        {
            if (hubConnection != null && hubConnection.State == HubConnectionState.Disconnected)
            {
                try
                {
                    await hubConnection.StartAsync();

                    // 🚀 قراءة أرقام الشرايح من ذاكرة الموبايل (لو مفيش، هيفترض 1,2 كديفولت مؤقت)
                    string savedSims = Microsoft.Maui.Storage.Preferences.Default.Get("MySimIds", "1,2");

                    if (!string.IsNullOrWhiteSpace(savedSims))
                    {
                        // تحويل النص (مثال: "3,4") إلى قائمة أرقام
                        var mySims = savedSims.Split(',').Select(int.Parse).ToList();

                        // إرسال الأرقام الخاصة بهذا الهاتف للسيرفر
                        await hubConnection.InvokeAsync("RegisterMobile", mySims);
                        System.Diagnostics.Debug.WriteLine($"[TeleCore] 📱 Registered SIMs: {savedSims}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Connection Error: {ex.Message}");
                }
            }
        }

        // 🎯 دالة جديدة: دي اللي هنناديها لما الكاشير يكتب أرقام الشرايح ويدوس "حفظ"
        public async Task SaveSimsAndReconnectAsync(string simIdsCommaSeparated)
        {
            // 1. حفظ الأرقام في ذاكرة الموبايل
            Microsoft.Maui.Storage.Preferences.Default.Set("MySimIds", simIdsCommaSeparated);

            // 2. لو الموبايل متصل حالياً، نفصله عشان يتصل من جديد بالأرقام الجديدة
            if (IsConnected)
            {
                await hubConnection.StopAsync();
            }

            // 3. إعادة الاتصال (واللي بدورها هتقرأ الأرقام الجديدة وتبعتهاللسيرفر)
            await ConnectAsync();
        }

        // =====================================
        // الكلاسات المساعدة للرسائل والبيانات
        // =====================================
        public class RemoteOrderMessage : CommunityToolkit.Mvvm.Messaging.Messages.ValueChangedMessage<RemoteOrder>
        {
            public RemoteOrderMessage(RemoteOrder value) : base(value) { }
        }

        public class RemoteOrder
        {
            public int SimId { get; set; }
            public string TargetNumber { get; set; }
            public double Amount { get; set; }
            public int TransactionId { get; set; }
        }
    }
}