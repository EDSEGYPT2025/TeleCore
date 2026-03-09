using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TeleCore.Application.Common;
using TeleCore.Domain.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TeleCore.APIServer.Pages
{
    public class ManageNodesModel : PageModel
    {
        private readonly IApplicationDbContext _context;

        public ManageNodesModel(IApplicationDbContext context)
        {
            _context = context;
        }

        public List<MobileNode> Nodes { get; set; } = new();
        public List<SelectListItem> AvailableBranches { get; set; } = new();
        public List<SelectListItem> AvailableSims { get; set; } = new();

        // 🟢 التعديل الاستراتيجي: الخريطة أصبحت تحتفظ بقائمة (List) من الشرائح لكل جهاز بدلاً من شريحة واحدة
        public Dictionary<int, List<SimCard>> NodeSimMappings { get; set; } = new();

        [TempData]
        public string SystemMessage { get; set; }
        [TempData]
        public string MessageType { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadDataAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAddNodeAsync(string deviceId, string deviceModel)
        {
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                SystemMessage = "يجب إدخال المعرف الفريد للجهاز (Device ID).";
                MessageType = "danger";
                return RedirectToPage();
            }

            var exists = await _context.MobileNodes.AnyAsync(n => n.DeviceUniqueId == deviceId);
            if (exists)
            {
                SystemMessage = "هذا الجهاز موجود بالفعل على الرادار!";
                MessageType = "warning";
                return RedirectToPage();
            }

            var newNode = new MobileNode
            {
                DeviceUniqueId = deviceId,
                DeviceModel = string.IsNullOrWhiteSpace(deviceModel) ? "Unknown Device" : deviceModel,
                PublicKey = "N/A",
                IsAuthorized = false,
                LastSeen = DateTime.UtcNow
            };

            _context.MobileNodes.Add(newNode);
            await _context.SaveChangesAsync(default);

            SystemMessage = $"تم التقاط الجهاز {deviceId} بنجاح. يرجى التصريح له.";
            MessageType = "success";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostToggleAuthAsync(int id)
        {
            var node = await _context.MobileNodes.FindAsync(id);
            if (node == null) return NotFound();

            node.IsAuthorized = !node.IsAuthorized;
            await _context.SaveChangesAsync(default);

            SystemMessage = node.IsAuthorized ? $"تم التصريح للجهاز: {node.DeviceModel}" : $"تم سحب الصلاحية من: {node.DeviceModel}";
            MessageType = node.IsAuthorized ? "success" : "warning";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostAssignBranchAsync(int nodeId, int? branchId)
        {
            var node = await _context.MobileNodes.FindAsync(nodeId);
            if (node == null) return NotFound();

            node.BranchId = branchId;
            await _context.SaveChangesAsync(default);
            SystemMessage = "تم تحديث موقع الجهاز العسكري بنجاح.";
            MessageType = "success";
            return RedirectToPage();
        }

        // 🟢 تم تعديل الدالة لكي لا تمسح الشريحة القديمة (نظام التسليح المتعدد)
        public async Task<IActionResult> OnPostAssignSimAsync(int nodeId, int? simId)
        {
            if (simId.HasValue)
            {
                var newSim = await _context.SimCards.FindAsync(simId.Value);
                if (newSim != null) newSim.MobileNodeId = nodeId;
                await _context.SaveChangesAsync(default);

                SystemMessage = "تم تسليح الجهاز بالشريحة كخط دعم إضافي.";
                MessageType = "success";
            }
            return RedirectToPage();
        }

        // 🟢 دالة جديدة لسحب شريحة معينة من الجهاز بأسلوب جراحي
        public async Task<IActionResult> OnPostUnassignSimAsync(int simId)
        {
            var sim = await _context.SimCards.FindAsync(simId);
            if (sim != null)
            {
                sim.MobileNodeId = null;
                await _context.SaveChangesAsync(default);
                SystemMessage = "تم سحب الشريحة من الجهاز بنجاح.";
                MessageType = "warning";
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteNodeAsync(int id)
        {
            var node = await _context.MobileNodes.FindAsync(id);
            if (node != null)
            {
                // فك ارتباط كل الشرائح المربوطة بهذا الجهاز قبل تدميره
                var linkedSims = await _context.SimCards.Where(s => s.MobileNodeId == id).ToListAsync();
                foreach (var sim in linkedSims) sim.MobileNodeId = null;

                _context.MobileNodes.Remove(node);
                await _context.SaveChangesAsync(default);
                SystemMessage = "تم تدمير عقدة الاتصال بنجاح.";
                MessageType = "danger";
            }
            return RedirectToPage();
        }

        private async Task LoadDataAsync()
        {
            Nodes = await _context.MobileNodes
                .Include(n => n.Branch)
                .OrderByDescending(n => n.LastSeen)
                .ToListAsync();

            AvailableBranches = await _context.Branches
                .Where(b => b.IsActive)
                .Select(b => new SelectListItem { Value = b.Id.ToString(), Text = b.Name })
                .ToListAsync();

            // 🟢 جلب الشرائح الحرة فقط (غير المربوطة بأي جهاز) لكي لا تظهر نفس الشريحة مرتين
            AvailableSims = await _context.SimCards
                .Where(s => s.IsActive && s.MobileNodeId == null)
                .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = $"{s.PhoneNumber} ({s.Provider})" })
                .ToListAsync();

            // 🟢 بناء خريطة الربط للقائمة الجديدة (تجميع الشرائح حسب رقم الجهاز)
            var assignedSims = await _context.SimCards
                .Where(s => s.MobileNodeId != null && s.IsActive)
                .ToListAsync();

            NodeSimMappings = assignedSims
                .GroupBy(s => s.MobileNodeId.Value)
                .ToDictionary(g => g.Key, g => g.ToList());
        }
    }
}