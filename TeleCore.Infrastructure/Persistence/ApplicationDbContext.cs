using Microsoft.EntityFrameworkCore;
using TeleCore.Application.Common;
using TeleCore.Domain.Entities;

namespace TeleCore.Infrastructure.Persistence
{
    public class ApplicationDbContext : DbContext, IApplicationDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Company> Companies => Set<Company>();
        public DbSet<Branch> Branches => Set<Branch>();
        public DbSet<SimCard> SimCards => Set<SimCard>();
        public DbSet<Transaction> Transactions => Set<Transaction>();
        public DbSet<CommissionRule> CommissionRules => Set<CommissionRule>();
        public DbSet<Cashier> Cashiers => Set<Cashier>();
        public DbSet<Drawer> Drawers => Set<Drawer>();
        public DbSet<Shift> Shifts => Set<Shift>();
        public DbSet<ShiftSimCard> ShiftSimCards => Set<ShiftSimCard>();

        // الجدول الجديد الخاص بأجهزة الأندرويد
        public DbSet<MobileNode> MobileNodes => Set<MobileNode>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // إعدادات الـ Precision للعمليات المالية
            modelBuilder.Entity<Transaction>(entity => {
                entity.Property(e => e.Amount).HasPrecision(18, 2);
                entity.Property(e => e.Commission).HasPrecision(18, 2);
                entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            });

            // إعدادات ميزان المراجعة والرصيد في الجداول الأخرى
            modelBuilder.Entity<SimCard>(entity => {
                entity.Property(e => e.CurrentBalance).HasPrecision(18, 2);
                entity.Property(e => e.DailyLimit).HasPrecision(18, 2);
                entity.Property(e => e.MonthlyLimit).HasPrecision(18, 2);
            });

            modelBuilder.Entity<Drawer>(entity => {
                entity.Property(e => e.CurrentBalance).HasPrecision(18, 2);
            });

            // ربط الـ MobileNode بالـ Branch (One-to-Many)
            modelBuilder.Entity<MobileNode>()
                .HasOne(m => m.Branch)
                .WithMany() // يمكنك إضافة ICollection<MobileNode> في كلاس Branch لاحقاً
                .HasForeignKey(m => m.BranchId)
                .OnDelete(DeleteBehavior.SetNull);

            base.OnModelCreating(modelBuilder);
        }
    }
}