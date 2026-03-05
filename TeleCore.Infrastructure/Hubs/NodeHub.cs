using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TeleCore.Application.Common;
using TeleCore.Domain.Entities;

namespace TeleCore.Infrastructure.Hubs
{
    public class NodeHub : Hub
    {
        private readonly IApplicationDbContext _context;

        public NodeHub(IApplicationDbContext context)
        {
            _context = context;
        }

        // الموبايل بينادي الميثود دي أول ما يفتح
        public async Task RegisterNode(string deviceUniqueId, string model, string publicKey)
        {
            // 1. البحث عن الجهاز إذا كان مسجلاً مسبقاً
            var existingNode = await _context.MobileNodes
                .FirstOrDefaultAsync(n => n.DeviceUniqueId == deviceUniqueId);

            if (existingNode == null)
            {
                // 2. إذا كان جهاز جديد، نقوم بإضافته
                var newNode = new MobileNode
                {
                    DeviceUniqueId = deviceUniqueId,
                    DeviceModel = model,
                    PublicKey = publicKey,
                    IsAuthorized = false, // يحتاج موافقة يدوية من لوحة التحكم
                    LastSeen = DateTime.UtcNow
                };
                _context.MobileNodes.Add(newNode);
            }
            else
            {
                // 3. إذا كان مسجلاً، نحدث المفتاح العام (في حال مسح التطبيق وتنزيله مجدداً) ونحدث تاريخ التواجد
                existingNode.PublicKey = publicKey;
                existingNode.LastSeen = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            // 4. الرد على الموبايل بحالة التسجيل
            await Clients.Caller.SendAsync("RegistrationResponse", "Registered_Successfully_Wait_Approval");
        }
    }
}