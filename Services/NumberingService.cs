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
                .IgnoreQueryFilters()
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
                .IgnoreQueryFilters()
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

    // Meme logique fiable que les autres numerotations (basee sur le dernier code
    // trouve en base, pas sur un Count()) au lieu de la generation ad hoc qui
    // provoquait des collisions "UNIQUE constraint failed: Clients.Code".
    // IMPORTANT : IgnoreQueryFilters() est indispensable ici. AppDbContext applique
    // un filtre global HasQueryFilter(c => !c.IsDeleted) sur Client, qui masque les
    // clients supprimes dans TOUTE requete "normale". Mais l'index unique sur Code,
    // lui, s'applique a TOUTES les lignes (supprimees ou non). Sans IgnoreQueryFilters,
    // on ignore les codes deja pris par des clients supprimes et on les regenere,
    // ce qui provoque la collision UNIQUE constraint contre la ligne encore en base.
    public async Task<string> GetNextClientCodeAsync()
    {
        lock (_lock)
        {
            var prefix = "CLI-";
            var last = _context.Clients
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(c => c.Code.StartsWith(prefix))
                .OrderByDescending(c => c.Code)
                .FirstOrDefault();
            int next = 1;
            if (last != null && int.TryParse(last.Code.Replace(prefix, ""), out int lastNum))
                next = lastNum + 1;
            return $"{prefix}{next:D3}";
        }
    }
}
