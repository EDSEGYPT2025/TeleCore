using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TeleCore.Mobile.Models;
using TeleCore.Mobile.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Maui.ApplicationModel.DataTransfer;

namespace TeleCore.Mobile.Pages
{
    public partial class HistoryPage : ContentPage
    {
        private List<TransactionRecord> _allTransactions = new List<TransactionRecord>();

        public HistoryPage()
        {
            InitializeComponent();

            // الاشتراك في رسالة التحديث من الـ SmsReceiver (باستخدام Messenger الحديث)
            WeakReferenceMessenger.Default.Register<string>(this, (r, message) =>
            {
                if (message == "RefreshHistory")
                {
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        await LoadDataAsync();
                    });
                }
            });
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                var db = new DatabaseService();
                var data = await db.GetTransactionsAsync();

                // ترتيب العمليات من الأحدث للأقدم دائماً
                _allTransactions = data.OrderByDescending(t => t.Timestamp).ToList();

                // تحديث الواجهة بالفلتر الحالي (الكل افتراضياً)
                ApplyFilter("الكل", BtnAll);
            }
            catch (Exception ex)
            {
                await DisplayAlert("خطأ", "فشل تحميل البيانات: " + ex.Message, "موافق");
            }
        }

        private void OnFilterClicked(object sender, EventArgs e)
        {
            if (sender is Button clickedButton)
            {
                string filterType = clickedButton.CommandParameter?.ToString() ?? "الكل";
                ApplyFilter(filterType, clickedButton);
            }
        }

        private void ApplyFilter(string filterType, Button activeBtn)
        {
            // إعادة ضبط الاستايل
            ResetButtonStyles(BtnAll);
            ResetButtonStyles(BtnIncoming);
            ResetButtonStyles(BtnOutgoing);

            // تمييز الزر النشط
            activeBtn.BackgroundColor = Color.FromArgb("#0F172A");
            activeBtn.TextColor = Colors.White;
            activeBtn.BorderWidth = 0;

            if (filterType == "الكل")
            {
                TransactionsList.ItemsSource = _allTransactions;
            }
            else
            {
                TransactionsList.ItemsSource = _allTransactions.Where(t => t.Type == filterType).ToList();
            }
        }

        // --- ميزة المشاركة الذكية عند لمس الكارت ---
        private async void OnTransactionTapped(object sender, EventArgs e)
        {
            var frame = sender as Frame;
            var tx = frame?.BindingContext as TransactionRecord;
            if (tx == null) return;

            // إضافة لمسة احترافية: اهتزاز خفيف عند الضغط
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);

            // تجهيز الرقم
            string cleanNumber = tx.ReceiverNumber.Trim();
            if (cleanNumber.StartsWith("0")) cleanNumber = "2" + cleanNumber;

            // صياغة الإيصال بشكل جمالي (Business Style)
            string receiptText = $@"
*TeleCore Pay - إيصال دفع* 🚀
---
*الحالة:* {(tx.IsConfirmed ? "تمت بنجاح ✅" : "قيد المعالجة ⏳")}
*العملية:* {tx.Type}
*المبلغ:* {tx.Amount} ج.م
---
*الطرف الآخر:* {tx.ReceiverName}
*رقم المعاملة:* `{tx.TransactionId}`
*التاريخ:* {tx.Timestamp:yyyy/MM/dd HH:mm}
---
*شكراً لثقتكم بنا!*";

            string whatsappUrl = $"https://wa.me/{cleanNumber}?text={Uri.EscapeDataString(receiptText)}";

            try
            {
                await Launcher.Default.OpenAsync(whatsappUrl);
            }
            catch
            {
                await DisplayAlert("تنبيه", "تطبيق واتساب غير متوفر حالياً.", "موافق");
            }
        }
        private void ResetButtonStyles(Button btn)
        {
            btn.BackgroundColor = Colors.White;
            btn.TextColor = Color.FromArgb("#64748B");
            btn.BorderColor = Color.FromArgb("#E2E8F0");
            btn.BorderWidth = 1;
        }
    }
}