using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using TeleCore.Application.Common; // مسار الـ IApplicationDbContext

namespace TeleCore.APIServer.Hubs
{
    public class TransactionHub : Hub
    {
        public static readonly ConcurrentDictionary<int, string> _simConnections = new();
        public static readonly ConcurrentDictionary<int, string> _simPublicKeys = new();

        private readonly IApplicationDbContext _context;

        public TransactionHub(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task RegisterMobile(List<int> simIds, string publicKey)
        {
            foreach (var id in simIds)
            {
                _simConnections[id] = Context.ConnectionId;

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
                _simPublicKeys.TryRemove(item.Key, out _);
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

        // 🛑 تحديث الداتابيز المتوافق مع الـ Entity الخاصة بك
        public async Task UpdateTransactionStatus(string resultMessage)
        {
            // 1. إرسال النتيجة للوحة الكاشير لتظهر للمستخدم فوراً
            await Clients.All.SendAsync("ReceiveTransactionResult", resultMessage);
            Console.WriteLine($"[TeleCore] ✅ Result Received: {resultMessage}");

            // 2. تحديث قاعدة البيانات في الخلفية
            try
            {
                // استخراج رقم الموبايل من الرسالة (نبحث عن كلمة تبدأ بـ 01 وطولها 11)
                string[] words = resultMessage.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                string? targetNumber = words.FirstOrDefault(w => w.StartsWith("01") && w.Length == 11);

                if (!string.IsNullOrEmpty(targetNumber))
                {
                    // جلب أحدث عملية معلقة لهذا الرقم
                    var pendingTransaction = await _context.Transactions
                        .Where(t => t.TargetNumber == targetNumber && t.Status == "Pending")
                        .OrderByDescending(t => t.CreatedAt)
                        .FirstOrDefaultAsync();

                    if (pendingTransaction != null)
                    {
                        if (resultMessage.Contains("تم تحويل") || resultMessage.Contains("نجاح"))
                        {
                            // تغيير الحالة إلى مكتملة بناءً على المسميات في كلاسك
                            pendingTransaction.Status = "Completed";
                            await _context.SaveChangesAsync(default);
                            Console.WriteLine($"[TeleCore] 🗄️ DB Updated: Transaction ID {pendingTransaction.Id} is now Completed.");
                        }
                        else if (resultMessage.Contains("رصيدك غير كاف") || resultMessage.Contains("فشل") || resultMessage.Contains("عفوا"))
                        {
                            // تغيير الحالة إلى فاشلة
                            pendingTransaction.Status = "Failed";
                            await _context.SaveChangesAsync(default);
                            Console.WriteLine($"[TeleCore] 🗄️ DB Updated: Transaction ID {pendingTransaction.Id} is now Failed.");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"[TeleCore] ⚠️ No Pending transaction found in DB for number: {targetNumber}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TeleCore] ❌ Database Update Error: {ex.Message}");
            }
        }
    }
}