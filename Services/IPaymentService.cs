using Nomad2.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nomad2.Sorting;

namespace Nomad2.Services
{
    public interface IPaymentService
    {
        int PageSize { get; set; }
        Task<(List<Payment> payments, int totalCount)> GetPaymentsAsync(int page, string searchTerm, SortOption<PaymentSortOption> sortOption);
        Task<Payment> GetPaymentByIdAsync(string id);
        Task<bool> AddPaymentAsync(Payment payment);
        Task<bool> UpdatePaymentAsync(Payment payment);
        Task<bool> DeletePaymentAsync(string id);
        Task<bool> ClearAllPaymentsAsync();
        Task<string> GetLastPaymentIdAsync();
        Task<List<Payment>> GetAllPaymentsAsync();
        Task<int> GetTotalPaidForRentalAsync(string rentalId);
        Task ProcessRefundAsync(string paymentId, int refundAmount);
    }
} 