using TeleCore.Domain.Entities;

namespace TeleCore.Application.Services
{
    public interface ITransactionService
    {
        Task<Transaction> ExecuteTransferAsync(int simId, string targetNumber, decimal amount);
    }
}