using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
// جرب تضغط Ctrl+. على السطر اللي تحت لو لسه أحمر
using TeleCore.Infrastructure.Persistence;

namespace TeleCore.APIServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SimsController : ControllerBase
    {
        // تأكد أن اسم الكلاس هنا يطابق اسم ملف الـ Context عندك
        private readonly ApplicationDbContext _context;

        public SimsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("my-assigned-sims/{deviceId}")]
        public async Task<IActionResult> GetAssignedSims(string deviceId)
        {
            var simIds = await _context.SimCards
                .Where(s => s.AssignedDeviceId == deviceId && s.IsActive)
                .Select(s => s.Id)
                .ToListAsync();

            return Ok(simIds);
        }
    }
}