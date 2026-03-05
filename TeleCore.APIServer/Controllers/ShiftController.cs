using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeleCore.Domain.Entities;
using TeleCore.Infrastructure.Persistence;

[ApiController]
[Route("api/[controller]")]
public class ShiftController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ShiftController(ApplicationDbContext context)
    {
        _context = context;
    }

    // جلب كل الشرائح المتاحة في الفرع
    [HttpGet("available-sims/{branchId}")]
    public async Task<IActionResult> GetAvailableSims(int branchId)
    {
        var sims = await _context.SimCards
            .Where(s => s.BranchId == branchId && s.IsActive)
            .ToListAsync();
        return Ok(sims);
    }

    // فتح وردية جديدة
    [HttpPost("open-shift")]
    public async Task<IActionResult> OpenShift([FromBody] OpenShiftRequest request)
    {
        var shift = new Shift
        {
            CashierId = request.CashierId,
            DrawerId = request.DrawerId,
            StartTime = DateTime.Now,
            OpeningDrawerBalance = request.OpeningCash,
            IsClosed = false
        };

        foreach (var simId in request.SelectedSimIds)
        {
            var sim = await _context.SimCards.FindAsync(simId);
            shift.ShiftSimCards.Add(new ShiftSimCard
            {
                SimCardId = simId,
                OpeningBalance = sim.CurrentBalance // رصيد البداية
            });
        }

        _context.Shifts.Add(shift);
        await _context.SaveChangesAsync();

        return Ok(new { shiftId = shift.Id, message = "الوردية فتحت يا بطل!" });
    }

    [HttpPost("close-shift")]
    public async Task<IActionResult> CloseShift([FromBody] CloseShiftRequest request)
    {
        var shift = await _context.Shifts
            .Include(s => s.ShiftSimCards)
            .FirstOrDefaultAsync(s => s.Id == request.ShiftId && !s.IsClosed);

        if (shift == null) return NotFound("الوردية غير موجودة أو مغلقة بالفعل");

        shift.EndTime = DateTime.Now;
        shift.IsClosed = true;
        shift.ClosingDrawerBalance = request.ActualCash; // المبلغ اللي الكاشير سلمه فعلياً

        // حساب العجز أو الزيادة في الكاش
        // (الرصيد الافتتاحي + إجمالي العمليات الكاش - المصاريف إن وجد)
        shift.ShortageOrSurplus = shift.ClosingDrawerBalance - shift.OpeningDrawerBalance;

        // تحديث أرصدة الشرايح عند الإغلاق
        foreach (var simStatus in request.SimClosingBalances)
        {
            var shiftSim = shift.ShiftSimCards.FirstOrDefault(x => x.SimCardId == simStatus.SimId);
            if (shiftSim != null)
            {
                shiftSim.ClosingBalance = simStatus.ActualBalance;

                // تحديث الرصيد الحالي في جدول الشرايح الأساسي
                var mainSim = await _context.SimCards.FindAsync(simStatus.SimId);
                if (mainSim != null) mainSim.CurrentBalance = simStatus.ActualBalance;
            }
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "تم إغلاق الوردية وحفظ تقرير العهدة" });
    }

    public class CloseShiftRequest
    {
        public int ShiftId { get; set; }
        public decimal ActualCash { get; set; }
        public List<SimClosingStatus> SimClosingBalances { get; set; }
    }

    public class SimClosingStatus
    {
        public int SimId { get; set; }
        public decimal ActualBalance { get; set; }
    }
}

public class OpenShiftRequest
{
    public int CashierId { get; set; }
    public int DrawerId { get; set; }
    public decimal OpeningCash { get; set; }
    public List<int> SelectedSimIds { get; set; }
}