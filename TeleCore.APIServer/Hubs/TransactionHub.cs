using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using TeleCore.Application.Common; // مسار الـ IApplicationDbContext
using System.Text.RegularExpressions; // تأكد من إضافة هذا في أعلى الملف

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
                // تأكيد إضافي للشاشة لضمان التوافق مع الكود السابق
                await Clients.All.SendAsync("NodeStatusChanged", id, "Online");
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
        Console.WriteLine($"[TeleCore] 📩 Raw Result Received: {resultMessage}");

        try
        {
            // 1. استخدام Regex لالتقاط أي رقم موبايل (11 رقم يبدأ بـ 01) من وسط النص
            var match = Regex.Match(resultMessage, @"01\d{9}");
            string? targetNumber = match.Success ? match.Value : null;

            string finalStatus = "Failed";
            string finalTarget = targetNumber ?? "UNKNOWN";
            string finalAmount = "0.00";

            if (!string.IsNullOrEmpty(targetNumber))
            {
                // 2. البحث عن العملية (المعلقة Pending) أو (التي تحت المعالجة Processing)
                // أضفنا Processing لضمان التقاط العملية إذا كان الموبايل غير حالتها مسبقاً
                var pendingTransaction = await _context.Transactions
                    .Where(t => t.TargetNumber == targetNumber && (t.Status == "Pending" || t.Status == "Processing"))
                    .OrderByDescending(t => t.CreatedAt)
                    .FirstOrDefaultAsync();

                if (pendingTransaction != null)
                {
                    finalTarget = pendingTransaction.TargetNumber;
                    finalAmount = pendingTransaction.Amount.ToString("N2");

                    // فحص الكلمات الدليلة للنجاح
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
                else
                {
                    // إذا لم نجد Pending، ربما هي رسالة مكررة لعملية اكتملت بالفعل
                    var completedTx = await _context.Transactions
                        .Where(t => t.TargetNumber == targetNumber)
                        .OrderByDescending(t => t.CreatedAt)
                        .FirstOrDefaultAsync();

                    if (completedTx != null)
                    {
                        finalTarget = completedTx.TargetNumber;
                        finalAmount = completedTx.Amount.ToString("N2");
                        finalStatus = completedTx.Status;
                    }
                }
            }

            // 3. إرسال البيانات النهائية
            await Clients.All.SendAsync("ReceiveTransactionResult", finalStatus, resultMessage, finalTarget, finalAmount);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TeleCore] ❌ Error: {ex.Message}");
        }
    }        // داخل الـ Hub
}
}