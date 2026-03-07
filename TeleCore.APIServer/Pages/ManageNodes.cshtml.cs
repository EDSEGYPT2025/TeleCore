using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TeleCore.Application.Common;
using TeleCore.Domain.Entities;

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

        // 🗺️ خريطة لتخزين الشريحة المرتبطة بكل جهاز (مفتاح: رقم الجهاز، قيمة: رقم الشريحة)
        public Dictionary<int, string> NodeSimMappings { get; set; } = new();

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

        public async Task<IActionResult> OnPostAssignSimAsync(int nodeId, int? simId)
        {
            var oldSim = await _context.SimCards.FirstOrDefaultAsync(s => s.MobileNodeId == nodeId);
            if (oldSim != null) oldSim.MobileNodeId = null;

            if (simId.HasValue)
            {
                var newSim = await _context.SimCards.FindAsync(simId.Value);
                if (newSim != null) newSim.MobileNodeId = nodeId;
            }

            await _context.SaveChangesAsync(default);
            SystemMessage = "تم تسليح الجهاز بالشريحة بنجاح.";
            MessageType = "success";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteNodeAsync(int id)
        {
            var node = await _context.MobileNodes.FindAsync(id);
            if (node != null)
            {
                var linkedSim = await _context.SimCards.FirstOrDefaultAsync(s => s.MobileNodeId == id);
                if (linkedSim != null) linkedSim.MobileNodeId = null;

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

            AvailableSims = await _context.SimCards
                .Where(s => s.IsActive)
                .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = $"{s.PhoneNumber} ({s.Provider})" })
                .ToListAsync();

            // 🗺️ بناء خريطة الربط لإرسالها للواجهة بأمان
            var assignedSims = await _context.SimCards
                .Where(s => s.MobileNodeId != null && s.IsActive)
                .ToListAsync();

            foreach (var sim in assignedSims)
            {
                NodeSimMappings[sim.MobileNodeId.Value] = sim.Id.ToString();
            }
        }
    }
}