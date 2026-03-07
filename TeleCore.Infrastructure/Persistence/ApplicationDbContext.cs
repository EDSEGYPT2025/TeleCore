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
        public DbSet<MobileNode> MobileNodes => Set<MobileNode>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // 1. إعدادات الـ Precision لجدول العمليات
            modelBuilder.Entity<Transaction>(entity => {
                entity.Property(e => e.Amount).HasPrecision(18, 2);
                entity.Property(e => e.Commission).HasPrecision(18, 2);
                entity.Property(e => e.NetworkFee).HasPrecision(18, 2); // الحقل الجديد
                entity.Property(e => e.TotalAmount).HasPrecision(18, 2);

                // منع الحذف المتسلسل للعمليات لو اتمسحت الشريحة
                entity.HasOne(t => t.SimCard)
                      .WithMany()
                      .HasForeignKey(t => t.SimCardId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // 2. إعدادات الـ Precision لجدول الشرائح (الحدود المالية)
            modelBuilder.Entity<SimCard>(entity => {
                entity.Property(e => e.CurrentBalance).HasPrecision(18, 2);
                entity.Property(e => e.DailyLimit).HasPrecision(18, 2);
                entity.Property(e => e.MonthlyLimit).HasPrecision(18, 2);

                // الحدود الجديدة المفصلة
                entity.Property(e => e.DailyWithdrawLimit).HasPrecision(18, 2);
                entity.Property(e => e.MonthlyWithdrawLimit).HasPrecision(18, 2);
                entity.Property(e => e.DailyDepositLimit).HasPrecision(18, 2);
                entity.Property(e => e.MonthlyDepositLimit).HasPrecision(18, 2);
            });

            // 3. إعدادات الدرج والفرع
            modelBuilder.Entity<Drawer>(entity => {
                entity.Property(e => e.CurrentBalance).HasPrecision(18, 2);
            });

            modelBuilder.Entity<Branch>(entity => {
                entity.Property(e => e.DrawerCashBalance).HasPrecision(18, 2);
            });

            // 4. إعدادات قواعد العمولات
            modelBuilder.Entity<CommissionRule>(entity => {
                entity.Property(e => e.MinAmount).HasPrecision(18, 2);
                entity.Property(e => e.MaxAmount).HasPrecision(18, 2);
                entity.Property(e => e.FixedFee).HasPrecision(18, 2);
                // النسبة المئوية يفضل أن تكون 4 خانات عشرية (مثال: 0.0150) لمزيد من الدقة
                entity.Property(e => e.PercentageFee).HasPrecision(18, 4);
                entity.Property(e => e.RoundingStep).HasPrecision(18, 2);
            });

            // 5. إعدادات الوردية (الشيفت)
            modelBuilder.Entity<Shift>(entity => {
                entity.Property(e => e.OpeningDrawerBalance).HasPrecision(18, 2);
                entity.Property(e => e.ClosingDrawerBalance).HasPrecision(18, 2);
                entity.Property(e => e.ShortageOrSurplus).HasPrecision(18, 2);
            });

            // 6. إعدادات تفاصيل خطوط الوردية
            modelBuilder.Entity<ShiftSimCard>(entity => {
                entity.Property(e => e.OpeningBalance).HasPrecision(18, 2);
                entity.Property(e => e.ClosingBalance).HasPrecision(18, 2);

                // 🛑 منع الـ Cascade Delete الذي يسبب أخطاء الـ (Multiple cascade paths) في SQL Server
                entity.HasOne(sc => sc.Shift)
                      .WithMany(s => s.ShiftSimCards)
                      .HasForeignKey(sc => sc.ShiftId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(sc => sc.SimCard)
                      .WithMany()
                      .HasForeignKey(sc => sc.SimCardId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // 7. ربط الـ MobileNode بالـ Branch
            modelBuilder.Entity<MobileNode>()
                .HasOne(m => m.Branch)
                .WithMany()
                .HasForeignKey(m => m.BranchId)
                .OnDelete(DeleteBehavior.SetNull);

            base.OnModelCreating(modelBuilder);
        }
    }
}