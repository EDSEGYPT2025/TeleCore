using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace TeleCore.APIServer.Hubs
{
    public class TransactionHub : Hub
    {
        public static readonly ConcurrentDictionary<int, string> _simConnections = new();

        // ✅ إضافة قاموس جديد لحفظ المفتاح العام لكل شريحة (سنحتاجه لاحقاً في التشفير)
        public static readonly ConcurrentDictionary<int, string> _simPublicKeys = new();

        // 🛑 التعديل الجوهري هنا: إضافة (string publicKey)
        public async Task RegisterMobile(List<int> simIds, string publicKey)
        {
            foreach (var id in simIds)
            {
                _simConnections[id] = Context.ConnectionId;

                // حفظ المفتاح
                if (!string.IsNullOrEmpty(publicKey))
                {
                    _simPublicKeys[id] = publicKey;
                }

                await Clients.All.SendAsync("UpdateSimStatus", id, true);
            }
            Console.WriteLine($"[TeleCore] 📱 Mobile Registered for SIMs: {string.Join(", ", simIds)}");
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var itemsToRemove = _simConnections.Where(x => x.Value == Context.ConnectionId).ToList();
            foreach (var item in itemsToRemove)
            {
                _simConnections.TryRemove(item.Key, out _);
                _simPublicKeys.TryRemove(item.Key, out _); // تنظيف المفتاح عند خروج الموبايل
                await Clients.All.SendAsync("UpdateSimStatus", item.Key, false);
            }
            await base.OnDisconnectedAsync(exception);
        }

        public async Task PingSim(int simId)
        {
            if (_simConnections.TryGetValue(simId, out string? connectionId))
            {
                await Clients.Client(connectionId).SendAsync("PingDevice");
            }
        }

        public Task<List<int>> GetOnlineSims()
        {
            return Task.FromResult(_simConnections.Keys.ToList());
        }

        public async Task SendTransferOrder(int simId, string targetNumber, double amount, string encryptedPin)
        {
            if (_simConnections.TryGetValue(simId, out string? connectionId))
            {
                await Clients.Client(connectionId).SendAsync("ReceiveSecureOrder", targetNumber, amount, encryptedPin);
                Console.WriteLine($"[TeleCore] 🚀 Order sent to SIM {simId}: {amount} to {targetNumber}");
            }
            else
            {
                Console.WriteLine($"[TeleCore] ❌ Failed to send order: SIM {simId} is offline.");
            }
        }

        public async Task UpdateTransactionStatus(string resultMessage)
        {
            await Clients.All.SendAsync("ReceiveTransactionResult", resultMessage);
            Console.WriteLine($"[TeleCore] ✅ Result Received: {resultMessage}");
        }
    }
}