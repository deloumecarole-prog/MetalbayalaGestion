using MetalBayalaGestion.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace MetalBayalaGestion.Services;

public class NumberingService : INumberingService
{
    private readonly AppDbContext _context;
    private readonly object _lock = new();

    public NumberingService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<string> GetNextQuoteNumberAsync()
    {
        lock (_lock)
        {
            var year = DateTime.Now.Year;
            var prefix = $"DEV-{year}-";
            var last = _context.Quotes
                .AsNoTracking()
                .Where(q => q.Number.StartsWith(prefix))
                .OrderByDescending(q => q.Number)
                .FirstOrDefault();
            int next = 1;
            if (last != null && int.TryParse(last.Number.Replace(prefix, ""), out int lastNum))
                next = lastNum + 1;
            return $"{prefix}{next:D4}";
        }
    }

    public async Task<string> GetNextInvoiceNumberAsync()
    {
        lock (_lock)
        {
            var year = DateTime.Now.Year;
            var prefix = $"FAC-{year}-";
            var last = _context.Invoices
                .AsNoTracking()
                .Where(i => i.Number.StartsWith(prefix))
                .OrderByDescending(i => i.Number)
                .FirstOrDefault();
            int next = 1;
            if (last != null && int.TryParse(last.Number.Replace(prefix, ""), out int lastNum))
                next = lastNum + 1;
            return $"{prefix}{next:D4}";
        }
    }

    public async Task<string> GetNextOrderNumberAsync()
    {
        lock (_lock)
        {
            var year = DateTime.Now.Year;
            var prefix = $"CMD-{year}-";
            var last = _context.Orders
                .AsNoTracking()
                .Where(o => o.Number.StartsWith(prefix))
                .OrderByDescending(o => o.Number)
                .FirstOrDefault();
            int next = 1;
            if (last != null && int.TryParse(last.Number.Replace(prefix, ""), out int lastNum))
                next = lastNum + 1;
            return $"{prefix}{next:D4}";
        }
    }

    public async Task<string> GetNextDeliveryNoteNumberAsync()
    {
        lock (_lock)
        {
            var year = DateTime.Now.Year;
            var prefix = $"BL-{year}-";
            var last = _context.DeliveryNotes
                .AsNoTracking()
                .Where(d => d.Number.StartsWith(prefix))
                .OrderByDescending(d => d.Number)
                .FirstOrDefault();
            int next = 1;
            if (last != null && int.TryParse(last.Number.Replace(prefix, ""), out int lastNum))
                next = lastNum + 1;
            return $"{prefix}{next:D4}";
        }
    }

    public async Task<string> GetNextPaymentNumberAsync()
    {
        lock (_lock)
        {
            var year = DateTime.Now.Year;
            var prefix = $"PAY-{year}-";
            var last = _context.Payments
                .AsNoTracking()
                .Where(p => p.Number.StartsWith(prefix))
                .OrderByDescending(p => p.Number)
                .FirstOrDefault();
            int next = 1;
            if (last != null && int.TryParse(last.Number.Replace(prefix, ""), out int lastNum))
                next = lastNum + 1;
            return $"{prefix}{next:D4}";
        }
    }
}
