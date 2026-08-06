using Microsoft.EntityFrameworkCore;
using MetalBayalaGestion.Models;

namespace MetalBayalaGestion.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Company> Companies { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Client> Clients { get; set; }
    public DbSet<Supplier> Suppliers { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<StockMovement> StockMovements { get; set; }
    public DbSet<Quote> Quotes { get; set; }
    public DbSet<QuoteLine> QuoteLines { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderLine> OrderLines { get; set; }
    public DbSet<DeliveryNote> DeliveryNotes { get; set; }
    public DbSet<DeliveryNoteLine> DeliveryNoteLines { get; set; }
    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<InvoiceLine> InvoiceLines { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Expense> Expenses { get; set; }
    public DbSet<CashTransaction> CashTransactions { get; set; }
    public DbSet<Setting> Settings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Client>().HasQueryFilter(c => !c.IsDeleted);
        modelBuilder.Entity<Supplier>().HasQueryFilter(s => !s.IsDeleted);
        modelBuilder.Entity<Product>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<Quote>().HasQueryFilter(q => !q.IsDeleted);
        modelBuilder.Entity<Invoice>().HasQueryFilter(i => !i.IsDeleted);

        modelBuilder.Entity<Client>().HasIndex(c => c.Code).IsUnique();
        modelBuilder.Entity<Supplier>().HasIndex(s => s.Code).IsUnique();
        modelBuilder.Entity<Product>().HasIndex(p => p.Reference).IsUnique();
        modelBuilder.Entity<Product>().HasIndex(p => p.Code).IsUnique();
        modelBuilder.Entity<Quote>().HasIndex(q => q.Number).IsUnique();
        modelBuilder.Entity<Invoice>().HasIndex(i => i.Number).IsUnique();
        modelBuilder.Entity<Order>().HasIndex(o => o.Number).IsUnique();
        modelBuilder.Entity<DeliveryNote>().HasIndex(d => d.Number).IsUnique();
        modelBuilder.Entity<Payment>().HasIndex(p => p.Number).IsUnique();
        modelBuilder.Entity<Setting>().HasIndex(s => s.Key).IsUnique();

        modelBuilder.Entity<Company>().HasData(new Company { Id = 1, Name = "Metal Bayala" });
        // NB: le seed de l'utilisateur "admin" est géré uniquement par DbInitializer (avec un vrai hash PBKDF2).
        // Le placeholder qui était ici bloquait toute connexion (hash invalide, jamais recalculé par DbInitializer
        // puisque le username "admin" existait déjà grâce à ce HasData).
    }
}
