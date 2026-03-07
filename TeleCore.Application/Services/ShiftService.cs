using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using TeleCore.Application.Common;
using TeleCore.Application.DTOs;
using TeleCore.Domain.Entities;

namespace TeleCore.Application.Services
{
    public class ShiftService : IShiftService
    {
        private readonly IApplicationDbContext _context;

        public ShiftService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<(bool Success, string Message, int? ShiftId)> OpenShiftAsync(OpenShiftRequest request)
        {
            // 1. التأكد أن الكاشير ليس لديه وردية مفتوحة بالفعل
            var openShiftExists = await _context.Shifts
                .AnyAsync(s => s.CashierId == request.CashierId && !s.IsClosed);

            if (openShiftExists)
            {
                return (false, "عفواً، لديك وردية مفتوحة بالفعل. يجب إغلاقها أولاً.", null);
            }

            // 2. التأكد من أن الدرج ليس مستخدماً في وردية أخرى مفتوحة الآن
            var drawerInUse = await _context.Shifts
                .AnyAsync(s => s.DrawerId == request.DrawerId && !s.IsClosed);

            if (drawerInUse)
            {
                return (false, "عفواً، هذا الدرج مستخدم حالياً من قبل كاشير آخر.", null);
            }

            // 3. إنشاء الوردية الجديدة
            var newShift = new Shift
            {
                CashierId = request.CashierId,
                DrawerId = request.DrawerId,
                StartTime = DateTime.UtcNow,
                OpeningDrawerBalance = request.OpeningCashBalance,
                IsClosed = false
            };

            _context.Shifts.Add(newShift);
            await _context.SaveChangesAsync(default); // نحفظ عشان ناخد الـ Shift Id

            // 4. ربط الخطوط المختارة بالوردية وأخذ "الرصيد الافتتاحي"
            if (request.SelectedSimCardIds != null && request.SelectedSimCardIds.Any())
            {
                // جلب بيانات الخطوط المختارة لمعرفة رصيدها الحالي
                var selectedSims = await _context.SimCards
                    .Where(sim => request.SelectedSimCardIds.Contains(sim.Id))
                    .ToListAsync();

                foreach (var sim in selectedSims)
                {
                    var shiftSimCard = new ShiftSimCard
                    {
                        ShiftId = newShift.Id,
                        SimCardId = sim.Id,
                        OpeningBalance = sim.CurrentBalance // 📸 لقطة للرصيد الحالي
                    };

                    _context.ShiftSimCards.Add(shiftSimCard);
                }

                await _context.SaveChangesAsync(default);
            }

            return (true, "تم فتح الوردية بنجاح!", newShift.Id);
        }
    }
}