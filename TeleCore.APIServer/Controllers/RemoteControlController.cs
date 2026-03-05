using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TeleCore.APIServer.Hubs;

namespace TeleCore.APIServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RemoteControlController : ControllerBase
    {
        private readonly IHubContext<TransactionHub> _hubContext;

        public RemoteControlController(IHubContext<TransactionHub> hubContext)
        {
            _hubContext = hubContext;
        }

        [HttpPost("send-command")]
        public async Task<IActionResult> SendCommand([FromBody] RemoteOrder order)
        {
            // 🛑 التعديل الأول: استخدام _simConnections بدلاً من _onlineSims
            var onlineIds = string.Join(", ", TransactionHub._simConnections.Keys);
            Console.WriteLine($"[Debug] Online Sims right now: {onlineIds}");
            Console.WriteLine($"[Debug] Cashier is asking for SimId: {order.SimId}");

            // 🛑 التعديل الثاني: استخدام _simConnections
            if (TransactionHub._simConnections.TryGetValue(order.SimId, out var connectionId))
            {
                // ✅ إكمال كود الإرسال الفعلي للموبايل
                await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveOrder", new
                {
                    SimId = order.SimId,
                    TargetNumber = order.TargetNumber,
                    Amount = order.Amount,
                    TransactionId = new Random().Next(1000, 9999) // رقم عشوائي للعملية
                });

                return Ok(new { message = "✅ تم الإرسال للموبايل بنجاح" });
            }

            return BadRequest($"❌ الموبايل غير متصل. المتاح حالياً هم: {onlineIds}");
        }
    }

    public class RemoteOrder
    {
        public int SimId { get; set; }
        public string TargetNumber { get; set; } = string.Empty;
        public double Amount { get; set; }
    }
}