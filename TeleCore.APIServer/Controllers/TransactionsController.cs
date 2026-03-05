using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using TeleCore.Application.Common;
using TeleCore.Infrastructure.Hubs;

namespace TeleCore.APIServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionsController : ControllerBase
    {
        private readonly IApplicationDbContext _context;
        private readonly IHubContext<NodeHub> _nodeHub;

        public TransactionsController(IApplicationDbContext context, IHubContext<NodeHub> nodeHub)
        {
            _context = context;
            _nodeHub = nodeHub;
        }

        public class SecureOrderRequest
        {
            public int BranchId { get; set; }
            public string TargetNumber { get; set; } = string.Empty;
            public decimal Amount { get; set; }
            public string ClearPin { get; set; } = string.Empty;
        }

        [HttpPost("TestSecureTransaction")]
        public async Task<IActionResult> TestSecureTransaction([FromBody] SecureOrderRequest req)
        {
            var mobileNode = await _context.MobileNodes
                .FirstOrDefaultAsync(n => n.BranchId == req.BranchId && n.IsAuthorized);

            if (mobileNode == null)
                return BadRequest("لا يوجد جهاز موبايل معتمد ومربوط بهذا الفرع.");

            using var rsa = new RSACryptoServiceProvider(2048);
            rsa.FromXmlString(mobileNode.PublicKey);

            var pinBytes = Encoding.UTF8.GetBytes(req.ClearPin);
            var encryptedBytes = rsa.Encrypt(pinBytes, false);

            string encryptedPinBase64 = Convert.ToBase64String(encryptedBytes);

            await _nodeHub.Clients.All.SendAsync("ReceiveSecureOrder", req.TargetNumber, req.Amount, encryptedPinBase64);

            return Ok(new { Message = "تم التشفير", EncryptedPayload = encryptedPinBase64 });
        }

        // الميثود السحرية لإضافة الشريحة للداتابيز
        [HttpGet("SeedSim")]
        public async Task<IActionResult> SeedSim()
        {
            var sim = new TeleCore.Domain.Entities.SimCard
            {
                BranchId = 1,
                PhoneNumber = "01099998888",
                Provider = "Vodafone",
                CurrentBalance = 5000,
                DailyLimit = 60000,
                MonthlyLimit = 200000,
                IsActive = true
            };

            _context.SimCards.Add(sim);
            await _context.SaveChangesAsync();

            return Ok("تمت إضافة الشريحة بنجاح لداتابيز السيرفر الحقيقية!");
        }
    }
}