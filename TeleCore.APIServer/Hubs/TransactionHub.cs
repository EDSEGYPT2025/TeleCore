using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace TeleCore.APIServer.Hubs
{
    public class TransactionHub : Hub
    {
        // 🛑 استخدمنا ConcurrentDictionary عشان يكون آمن تماماً مع الـ SignalR
        public static readonly ConcurrentDictionary<int, string> _simConnections = new();

        public async Task RegisterMobile(List<int> simIds)
        {
            foreach (var id in simIds)
            {
                // نربط الشريحة بالموبايل
                _simConnections[id] = Context.ConnectionId;

                // 🚀 نبلغ كل لوحات التحكم (الموقع) إن الشريحة دي بقت أونلاين (true)
                await Clients.All.SendAsync("UpdateSimStatus", id, true);
            }

            Console.WriteLine($"[TeleCore] 📱 Mobile Registered for SIMs: {string.Join(", ", simIds)}");
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            // مسح الشرايح المرتبطة بالموبايل اللي قفل
            var itemsToRemove = _simConnections.Where(x => x.Value == Context.ConnectionId).ToList();

            foreach (var item in itemsToRemove)
            {
                _simConnections.TryRemove(item.Key, out _);

                // 🚀 نبلغ كل لوحات التحكم إن الشريحة دي بقت أوفلاين (false)
                await Clients.All.SendAsync("UpdateSimStatus", item.Key, false);
            }

            await base.OnDisconnectedAsync(exception);
        }

        // 🎯 الدالة دي الموقع هيناديها لما تدوس على زرار "بحث عن الشريحة"
        public async Task PingSim(int simId)
        {
            // بندور هل الشريحة دي متصلة ليها موبايل دلوقتي؟
            if (_simConnections.TryGetValue(simId, out string? connectionId))
            {
                // بنبعت أمر الـ Ping للموبايل ده بس
                await Clients.Client(connectionId).SendAsync("PingDevice");
            }
        }

        // 🎯 دالة إضافية: الموقع بيناديها أول ما يفتح عشان يعرف مين متصل حالياً
        public Task<List<int>> GetOnlineSims()
        {
            return Task.FromResult(_simConnections.Keys.ToList());
        }
    }
}