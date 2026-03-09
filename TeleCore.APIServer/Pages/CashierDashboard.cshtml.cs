using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TeleCore.Application.Common;
using TeleCore.Application.Services;
using TeleCore.APIServer.Hubs;
using System.Text.Json;

namespace TeleCore.APIServer.Pages
{
    public class CashierDashboardModel : PageModel
    {
        private readonly ITransactionService _transactionService;
        private readonly IHubContext<TransactionHub> _hubContext;
        private readonly IApplicationDbContext _context;

        public CashierDashboardModel(
            ITransactionService transactionService,
            IHubContext<TransactionHub> hubContext,
            IApplicationDbContext context)
        {
            _transactionService = transactionService;
            _hubContext = hubContext;
            _context = context;
        }

        [BindProperty] public int SelectedSimId { get; set; }
        [BindProperty] public string TargetNumber { get; set; } = string.Empty;
        [BindProperty] public decimal Amount { get; set; }
        [BindProperty] public string ClearPin { get; set; } = string.Empty;

        public string StatusMessage { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }
        public List<SelectListItem> AvailableSims { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadSimCardsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await LoadSimCardsAsync();
            if (!ModelState.IsValid) return Page();

            try
            {
                // 1. تسجيل العملية في قاعدة البيانات
                var result = await _transactionService.ExecuteTransferAsync(SelectedSimId, TargetNumber, Amount, ClearPin);

                // 2. تجهيز البيانات كنص بدائي جداً يفهمه أي جهاز (مثال: "1,01010475455,50,105")
                string rawPayload = $"{SelectedSimId},{TargetNumber},{Amount.ToString(System.Globalization.CultureInfo.InvariantCulture)},{result.transaction.Id}";

                // 3. البث العشوائي الشامل (إلغاء الاعتماد على ConnectionId)
                await _hubContext.Clients.All.SendAsync("ReceiveRawOrder", rawPayload);

                StatusMessage = $"🚀 تم إرسال الأمر للرادار العام! المرجعية: {result.transaction.TransactionReference}";
                IsSuccess = true;

                TargetNumber = ""; Amount = 0; ClearPin = "";
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ فشل: {ex.Message}";
                IsSuccess = false;
            }

            return Page();
        }
        private async Task LoadSimCardsAsync()
        {
            AvailableSims = await _context.SimCards
                .Where(s => s.IsActive)
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = $"{s.Provider} - {s.PhoneNumber} (الرصيد: {s.CurrentBalance} ج.م)"
                }).ToListAsync();
        }
    }
}