using System.Threading.Tasks;

namespace MetalBayalaGestion.Services;

public interface INumberingService
{
    Task<string> GetNextQuoteNumberAsync();
    Task<string> GetNextInvoiceNumberAsync();
    Task<string> GetNextOrderNumberAsync();
    Task<string> GetNextDeliveryNoteNumberAsync();
    Task<string> GetNextPaymentNumberAsync();
    Task<string> GetNextClientCodeAsync();
}
