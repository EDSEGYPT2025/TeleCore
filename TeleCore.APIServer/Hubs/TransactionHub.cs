using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using TeleCore.Application.Common;

namespace TeleCore.APIServer.Hubs
{
    public class TransactionHub : Hub
    {
        // 🗺️ خرائط الربط اللحظية
        public static readonly ConcurrentDictionary<int, string> _simConnections = new();
        public static readonly ConcurrentDictionary<int, string> _simPublicKeys = new();

        private readonly IApplicationDbContext _context;

        public TransactionHub(IApplicationDbContext context)
        {
            _context = context;
        }

        // 🎯 تحديث دالة التسجيل لتعمل مع إرسال الموبايل الحالي (متغير واحد فقط)
        // 🛡️ يجب أن يكون التوقيع هكذا بالضبط ليتوافق مع الموبايل

        // 🛡️ استبدل دالة RegisterMobile في ملف TransactionHub.cs بهذا الكود
        public async Task RegisterMobile(List<int> simIds)
        {
            foreach (var id in simIds)
            {
                _simConnections[id] = Context.ConnectionId;

                // إرسال إشارة خضراء فورية للشاشة
                await Clients.All.SendAsync("NodeStatusChanged", id, "Online");
                await Clients.All.SendAsync("UpdateSimStatus", id, true);
            }
            Console.WriteLine($"[TeleCore] 📱 Mobile Registered for SIMs: {string.Join(", ", simIds)}");
        }

        // 🛑 عند قطع الاتصال: تنظيف الرادار فوراً
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var itemsToRemove = _simConnections.Where(x => x.Value == Context.ConnectionId).ToList();
            foreach (var item in itemsToRemove)
            {
                _simConnections.TryRemove(item.Key, out _);
                _simPublicKeys.TryRemove(item.Key, out _);

                await Clients.All.SendAsync("UpdateSimStatus", item.Key, false);
                await Clients.All.SendAsync("NodeStatusChanged", item.Key, "Offline");
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

        // 🎯 تحديث حالة العملية من الموبايل للسيرفر
        public async Task UpdateTransactionStatus(string resultMessage)
        {
            Console.WriteLine($"[TeleCore] 📩 Raw Result Received: {resultMessage}");

            try
            {
                // 1. التقاط رقم الموبايل من الرسالة
                var match = Regex.Match(resultMessage, @"01\d{9}");
                string? targetNumber = match.Success ? match.Value : null;

                string finalStatus = "Failed";
                string finalTarget = targetNumber ?? "UNKNOWN";
                string finalAmount = "0.00";

                if (!string.IsNullOrEmpty(targetNumber))
                {
                    // 2. البحث عن العملية المعلقة في قاعدة البيانات
                    var pendingTransaction = await _context.Transactions
                        .Where(t => t.TargetNumber == targetNumber && (t.Status == "Pending" || t.Status == "Processing"))
                        .OrderByDescending(t => t.CreatedAt)
                        .FirstOrDefaultAsync();

                    if (pendingTransaction != null)
                    {
                        finalTarget = pendingTransaction.TargetNumber;
                        finalAmount = pendingTransaction.Amount.ToString("N2");

                        if (resultMessage.Contains("تحويل") || resultMessage.Contains("نجاح") || resultMessage.Contains("تم"))
                        {
                            finalStatus = "Completed";
                            pendingTransaction.Status = "Completed";
                        }
                        else
                        {
                            finalStatus = "Failed";
                            pendingTransaction.Status = "Failed";
                        }

                        await _context.SaveChangesAsync(default);
                        Console.WriteLine($"[TeleCore] 🗄️ DB Updated: ID {pendingTransaction.Id} is now {finalStatus}.");
                    }
                }

                // 3. إرسال النتيجة النهائية للوحة الكاشير لتحديث الشاشة فوراً
                await Clients.All.SendAsync("ReceiveTransactionResult", finalStatus, resultMessage, finalTarget, finalAmount);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TeleCore] ❌ Error in UpdateTransactionStatus: {ex.Message}");
            }
        }
    }
}