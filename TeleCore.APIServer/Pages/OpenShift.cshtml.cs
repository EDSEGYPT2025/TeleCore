using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TeleCore.Application.Common;
using TeleCore.Application.DTOs;
using TeleCore.Application.Services;
using TeleCore.Domain.Entities;

namespace TeleCore.APIServer.Pages
{
    public class OpenShiftModel : PageModel
    {
        private readonly IApplicationDbContext _context;
        private readonly IShiftService _shiftService;

        public OpenShiftModel(IApplicationDbContext context, IShiftService shiftService)
        {
            _context = context;
            _shiftService = shiftService;
        }

        [BindProperty]
        public TeleCore.Application.DTOs.OpenShiftRequest ShiftRequest { get; set; } = new TeleCore.Application.DTOs.OpenShiftRequest();

        public List<SelectListItem> AvailableDrawers { get; set; } = new();
        public List<SimCard> AvailableSimCards { get; set; } = new();

        public string StatusMessage { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadFormDataAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadFormDataAsync();
                return Page();
            }

            // للتجربة حالياً: نفترض أن الكاشير رقم 1 هو من يقوم بتسجيل الدخول
            // لاحقاً سيتم أخذ هذا الرقم من الـ (User Session / Cookie)
            ShiftRequest.CashierId = 1;

            // استدعاء خدمة فتح الوردية التي برمجناها سابقاً
            var result = await _shiftService.OpenShiftAsync(ShiftRequest);

            IsSuccess = result.Success;
            StatusMessage = result.Message;

            if (result.Success)
            {
                // توجيه الكاشير إلى لوحة العمليات بعد فتح الوردية بنجاح
                // (يمكنك تغيير "CashierDashboard" لاسم الصفحة الفعلية عندك)
                return RedirectToPage("/CashierDashboard");
            }

            await LoadFormDataAsync();
            return Page();
        }

        private async Task LoadFormDataAsync()
        {
            // جلب الأدراج المتاحة
            AvailableDrawers = await _context.Drawers
                .Where(d => d.IsActive)
                .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Name })
                .ToListAsync();

            // جلب الخطوط المتاحة لعرضها كـ Checkboxes
            AvailableSimCards = await _context.SimCards
                .Where(s => s.IsActive)
                .ToListAsync();
        }
    }
}