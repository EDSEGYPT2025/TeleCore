using Microsoft.EntityFrameworkCore;
using TeleCore.Domain.Entities; 

namespace TeleCore.Application.Common
{
    public interface IApplicationDbContext
    {
        DbSet<Company> Companies { get; }
        DbSet<Branch> Branches { get; }
        DbSet<SimCard> SimCards { get; }
        DbSet<Transaction> Transactions { get; }
        DbSet<MobileNode> MobileNodes { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}