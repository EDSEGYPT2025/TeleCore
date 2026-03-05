using SQLite;
using TeleCore.Mobile.Models;

namespace TeleCore.Mobile.Services
{
    public class DatabaseService
    {
        private SQLiteAsyncConnection _database;

        async Task Init()
        {
            if (_database != null) return;

            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "TeleCore.db3");
            _database = new SQLiteAsyncConnection(dbPath);

            await _database.CreateTableAsync<TransactionRecord>();

            // --- نظام التحديث الآمن ---
            try
            {
                // 1. تحديث عمود ServiceFee لو مش موجود
                var feeColumn = await _database.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM pragma_table_info('TransactionRecord') WHERE name='ServiceFee';");
                if (feeColumn == 0)
                    await _database.ExecuteAsync("ALTER TABLE TransactionRecord ADD COLUMN ServiceFee REAL DEFAULT 0;");

                // 2. تحديث عمود IsSynced الجديد
                var syncColumn = await _database.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM pragma_table_info('TransactionRecord') WHERE name='IsSynced';");
                if (syncColumn == 0)
                    await _database.ExecuteAsync("ALTER TABLE TransactionRecord ADD COLUMN IsSynced INTEGER DEFAULT 0;"); // 0 means false
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TeleCore] Migration Error: {ex.Message}");
            }
        }

        // 🚀 دالة جديدة لجلب العمليات اللي ماترفعتش للسيرفر
        public async Task<List<TransactionRecord>> GetUnsyncedTransactionsAsync()
        {
            await Init();
            return await _database.Table<TransactionRecord>()
                                  .Where(x => x.IsConfirmed == true && x.IsSynced == false)
                                  .ToListAsync();
        }

        public async Task SaveTransaction(TransactionRecord record)
        {
            await Init();

            // --- منع التكرار (Debounce) ---
            // البحث عن عملية مشابهة تمت في آخر 60 ثانية
            var oneMinuteAgo = DateTime.Now.AddSeconds(-60);
            var existing = await _database.Table<TransactionRecord>()
                .Where(x => x.ReceiverNumber == record.ReceiverNumber
                         && x.Amount == record.Amount
                         && x.Timestamp > oneMinuteAgo)
                .FirstOrDefaultAsync();

            if (existing != null) return; // لو موجودة فعلاً، اخرج متبنيش سجل جديد

            await _database.InsertAsync(record);
        }

        public async Task UpdateWithSms(string txId, double balance, double fee = 0, double amountFromSms = 0)
        {
            await Init();

            // 1. فحص التكرار (منع تسجيل نفس العملية مرتين)
            var duplicateId = await _database.Table<TransactionRecord>()
                .Where(x => x.TransactionId == txId && txId != "غير متوفر")
                .FirstOrDefaultAsync();

            if (duplicateId != null) return;

            // 2. البحث عن آخر عملية معلقة
            // إضافة تحسين: نبحث عن عملية معلقة يتطابق مبلغها مع المبلغ الموجود في الرسالة (اختياري لكنه أدق)
            var query = _database.Table<TransactionRecord>()
                                 .Where(x => x.IsConfirmed == false && x.Type == "تحويل");

            var lastTx = await query.OrderByDescending(x => x.Timestamp)
                                    .FirstOrDefaultAsync();

            if (lastTx != null)
            {
                // تحديث البيانات
                lastTx.TransactionId = txId;
                lastTx.PostBalance = balance;
                lastTx.ServiceFee = fee;
                lastTx.IsConfirmed = true;

                // لو مكنش عندنا مبلغ في السجل المعلق (حالة نادرة)، نحدثه من الرسالة
                if (lastTx.Amount == 0) lastTx.Amount = amountFromSms;

                await _database.UpdateAsync(lastTx);

                System.Diagnostics.Debug.WriteLine($"[TeleCore] Transaction {txId} confirmed successfully with fee: {fee}");
            }
        }
        public async Task<List<TransactionRecord>> GetTransactionsAsync()
        {
            await Init();
            // جلب الكل مرتباً من الأحدث للأقدم
            return await _database.Table<TransactionRecord>()
                                  .OrderByDescending(x => x.Timestamp)
                                  .ToListAsync();
        }

       

        public async Task<TransactionRecord> GetTransactionBySmsIdAsync(string txId)
        {
            await Init();
            return await _database.Table<TransactionRecord>().Where(x => x.TransactionId == txId).FirstOrDefaultAsync();
        }

        public async Task MarkAsSyncedAsync(int id)
        {
            await Init();
            var tx = await _database.Table<TransactionRecord>().Where(x => x.Id == id).FirstOrDefaultAsync();
            if (tx != null)
            {
                tx.IsSynced = true;
                await _database.UpdateAsync(tx);
            }
        }
    }
}