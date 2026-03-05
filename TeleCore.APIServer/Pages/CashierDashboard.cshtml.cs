using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TeleCore.Application.Common;
using TeleCore.Application.Services;
using TeleCore.APIServer.Hubs; // تأكد من استيراد مسار الـ Hub الصحيح

namespace TeleCore.APIServer.Pages
{
    public class CashierDashboardModel : PageModel
    {
        private readonly ITransactionService _transactionService;
        private readonly IHubContext<TransactionHub> _hubContext; // تم التغيير لـ TransactionHub
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
                // 1. تنفيذ العملية في الداتابيز وتشفير الـ PIN
                var result = await _transactionService.ExecuteTransferAsync(SelectedSimId, TargetNumber, Amount, ClearPin);

                // 2. البحث عن الـ ConnectionId الخاص بالموبايل الذي سجل برقم هذه الشريحة
                if (TransactionHub._simConnections.TryGetValue(SelectedSimId, out string? connectionId))
                {
                    // 3. إرسال الطلب للموبايل المحدد فقط (Targeted Sending)
                    // للتجربة فقط: أرسل البين الخام
                    await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveSecureOrder",
                        result.transaction.TargetNumber,
                        result.transaction.Amount,
                        ClearPin); // أرسل ClearPin بدلاً من result.encryptedPin

                    StatusMessage = $"🚀 تم الإرسال للموبايل بنجاح! رقم العملية: {result.transaction.TransactionReference}";
                    IsSuccess = true;

                    // تصفير الحقول بعد النجاح
                    TargetNumber = ""; Amount = 0; ClearPin = "";
                }
                else
                {
                    // هات كل الـ IDs اللي السيرفر عارفهم دلوقتي
                    var activeIds = string.Join(", ", TransactionHub._simConnections.Keys);
                    StatusMessage = $"⚠️ الموبايل غير متصل. الأجهزة النشطة حالياً هي: [{activeIds}]. تأكد أنك ضغطت Reconnect في الموبايل.";
                    IsSuccess = false;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ حدث خطأ أثناء التنفيذ: {ex.Message}";
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
                    Text = $"{s.Provider} - {s.PhoneNumber} (متوفر: {s.CurrentBalance} ج.م)"
                }).ToListAsync();
        }
    }
}