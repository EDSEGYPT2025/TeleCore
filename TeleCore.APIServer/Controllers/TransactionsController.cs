using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TeleCore.APIServer.Hubs;

namespace TeleCore.APIServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionsController : ControllerBase
    {
        private readonly IHubContext<TransactionHub> _hubContext;

        public TransactionsController(IHubContext<TransactionHub> hubContext)
        {
            _hubContext = hubContext;
        }

        [HttpGet("send-money")]
        public async Task<IActionResult> SendMoney(int simId, string targetNumber, double amount)
        {
            // 🛑 بننادي الـ Dictionary من الـ TransactionHub مباشرة
            if (TransactionHub._simConnections.TryGetValue(simId, out var connectionId))
            {
                // إرسال الأمر للموبايل
                await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveOrder", new
                {
                    SimId = simId,
                    TargetNumber = targetNumber,
                    Amount = amount,
                    TransactionId = new Random().Next(1000, 9999)
                });

                return Ok(new { message = "✅ تم إرسال الأمر للموبايل بنجاح" });
            }

            return NotFound(new { message = $"❌ الشريحة {simId} غير متصلة حالياً" });
        }


        [HttpPost("sync")]
        public async Task<IActionResult> SyncTransaction([FromBody] SyncTransactionRequest request)
        {
            // هنا المفروض بتكتب كود الـ Entity Framework لحفظ البيانات في قاعدة البيانات
            // _context.Transactions.Add(new Transaction { ... });
            // await _context.SaveChangesAsync();

            Console.WriteLine($"[TeleCore] 💾 تم مزامنة العملية {request.TransactionId} بمبلغ {request.Amount} ج.م بنجاح على السيرفر.");

            return Ok(new { message = "تمت المزامنة بنجاح" });
        }

        // الـ DTO اللي بيستقبل البيانات من الموبايل
        public class SyncTransactionRequest
        {
            public string Type { get; set; }
            public string ReceiverNumber { get; set; }
            public double Amount { get; set; }
            public string TransactionId { get; set; }
            public double PostBalance { get; set; }
            public double ServiceFee { get; set; }
            public DateTime Timestamp { get; set; }
        }

        [HttpGet("recent-targets")]
        public async Task<IActionResult> GetRecentTargetNumbers()
        {
            // هنا المفروض تستخدم الـ DbContext بتاعك
            // مثلاً: var numbers = await _context.Transactions
            //          .Select(t => t.TargetNumber)
            //          .Distinct()
            //          .Take(50)
            //          .ToListAsync();

            // للتبسيط والتجربة دلوقتي، هنرجع لستة وهمية لحد ما تربطها بالداتا بيز
            var recentNumbers = new List<object>
    {
        new { number = "01000000000", label = "01000000000 (مندوب الشركة)" },
        new { number = "01111292878", label = "01111292878 (رقم شخصي)" },
        new { number = "01234567890", label = "01234567890 (عميل دائم)" },
        new { number = "01555555555", label = "01555555555" }
    };

            return Ok(recentNumbers);
        }
    }
}