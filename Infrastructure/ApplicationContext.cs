using Microsoft.EntityFrameworkCore;

namespace Infrastructure;

public class ApplicationContext(DbContextOptions<ApplicationContext> opt) : DbContext(opt)
{
    public DbSet<Receipt> Receipts { get; set; }
    public DbSet<ReceiptItem> ReceiptItems { get; set; }
    public DbSet<ReceiptHistory> ReceiptHistories { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Receipt>(e =>
        {
            e.HasKey(e => e.Id);
            e.Property(e => e.Id).ValueGeneratedOnAdd();
            e.Property(e => e.Status).HasConversion<string>();

            e.HasMany(e => e.Items)
            .WithOne(e => e.Receipt)
            .HasForeignKey(e => e.ReceiptId);
        });

        builder.Entity<ReceiptItem>(e =>
        {
            e.HasKey(e => e.Id);
            e.Property(e => e.Id).ValueGeneratedOnAdd();
        });

        builder.Entity<ReceiptHistory>(e =>
        {
            e.HasKey(e => e.Id);
            e.Property(e => e.Id).ValueGeneratedOnAdd();

            e.HasOne(e => e.Receipt)
            .WithMany()
            .HasForeignKey(e => e.ReceiptId);
        });

        base.OnModelCreating(builder);
    }
}
