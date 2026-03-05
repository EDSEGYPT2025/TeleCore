using TeleCore.Domain.Entities;

namespace TeleCore.Application.Services
{
    public interface ITransactionService
    {
        // تم التعديل لإرجاع الـ Transaction بالإضافة إلى الـ PIN المشفر
        Task<(Transaction transaction, string encryptedPin)> ExecuteTransferAsync(int simId, string targetNumber, decimal amount, string clearPin);
    }
}