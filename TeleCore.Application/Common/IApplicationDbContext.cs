using Microsoft.EntityFrameworkCore;
using TeleCore.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace TeleCore.Application.Common
{
    public interface IApplicationDbContext
    {
        DbSet<Company> Companies { get; }
        DbSet<Branch> Branches { get; }
        DbSet<SimCard> SimCards { get; }
        DbSet<Transaction> Transactions { get; }
        DbSet<MobileNode> MobileNodes { get; }

        // ✅ الجداول الجديدة التي أضفناها
        DbSet<CommissionRule> CommissionRules { get; }
        DbSet<Cashier> Cashiers { get; }
        DbSet<Drawer> Drawers { get; }
        DbSet<Shift> Shifts { get; }
        DbSet<ShiftSimCard> ShiftSimCards { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    }
}