using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeleCore.Application.Common;
using TeleCore.Application.DTOs;
using TeleCore.Domain.Entities;

namespace TeleCore.APIServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DeviceController : ControllerBase
    {
        private readonly IApplicationDbContext _context;

        public DeviceController(IApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("Sync")]
        public async Task<IActionResult> Sync([FromBody] DeviceSyncRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.DeviceUniqueId))
            {
                return BadRequest(new DeviceSyncResponse
                {
                    Success = false,
                    Message = "INVALID_DEVICE_ID: لم يتم إرسال المعرف الخاص بالجهاز."
                });
            }

            // 1. البحث في الرادار (هل هذا الجهاز موجود لدينا؟)
            var node = await _context.MobileNodes
                .FirstOrDefaultAsync(n => n.DeviceUniqueId == request.DeviceUniqueId);

            // 🟢 الحالة الأولى والثانية (جهاز جديد، أو داتابيز ممسوحة)
            if (node == null)
            {
                node = new MobileNode
                {
                    DeviceUniqueId = request.DeviceUniqueId,
                    DeviceModel = string.IsNullOrWhiteSpace(request.DeviceModel) ? "Unknown" : request.DeviceModel,
                    IsAuthorized = false, // ينزل غير مصرح به تلقائياً (للحماية)
                    LastSeen = DateTime.UtcNow,
                    PublicKey = "N/A"
                };

                _context.MobileNodes.Add(node);
                await _context.SaveChangesAsync(default);

                return Ok(new DeviceSyncResponse
                {
                    Success = true,
                    IsAuthorized = false,
                    Message = "PENDING_APPROVAL: تم التقاط إشارة الجهاز. يرجى مراجعة لوحة تحكم الإدارة للتصريح."
                });
            }

            // 🟢 الحالة الثالثة (الجهاز موجود بالفعل)
            // تحديث وقت "آخر ظهور" لكي يظهر للمدير أنه "Online"
            node.LastSeen = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(request.DeviceModel) && node.DeviceModel != request.DeviceModel)
            {
                node.DeviceModel = request.DeviceModel; // تحديث اسم الجهاز إذا تغير
            }

            await _context.SaveChangesAsync(default);

            // 🛑 إذا كان الجهاز مقيداً (المسؤول لم يعطه تصريح بعد أو سحبه منه)
            if (!node.IsAuthorized)
            {
                return Ok(new DeviceSyncResponse
                {
                    Success = true,
                    IsAuthorized = false,
                    Message = "ACCESS_DENIED: الجهاز مقيد، في انتظار تصريح الإدارة."
                });
            }

            // ✅ الجهاز مصرح له! نبحث عن الشريحة التي ربطها المسؤول بهذا الجهاز
            var assignedSim = await _context.SimCards
                .FirstOrDefaultAsync(s => s.MobileNodeId == node.Id && s.IsActive);

            return Ok(new DeviceSyncResponse
            {
                Success = true,
                IsAuthorized = true,
                Message = "AUTHORIZED: الجهاز متصل وجاهز لتلقي الأوامر.",
                AssignedSimNumber = assignedSim?.PhoneNumber ?? "NO_SIM_ASSIGNED",
                Provider = assignedSim?.Provider ?? "N/A"
            });
        }
    }
}