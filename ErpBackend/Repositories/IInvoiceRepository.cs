using ErpBackend.Dtos;
using ErpBackend.Entities;

namespace ErpBackend.Repositories;

public interface IInvoiceRepository
{
    Task<IEnumerable<Invoice>> GetAllAsync();
    Task<Invoice> CreateInvoiceAsync(CreateInvoiceDto dto, string createdBy = "system");
    Task<Payment> AddPaymentAsync(int invoiceId, PaymentDto dto, string processedBy = "system");
}
