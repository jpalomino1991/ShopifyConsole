using Microsoft.EntityFrameworkCore;

namespace ShopifyConsole.Models
{
    public class AppContext : DbContext
    {
        string sqlConn;
        public AppContext(string sqlConn)
        {
            this.sqlConn = sqlConn;
        }

        public DbSet<Web> Web { get; set; }
        public DbSet<Product> Product { get; set; }
        public virtual DbSet<Stock> Stock { get; set; }
        public virtual DbSet<Price> Price { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Item> Item { get; set; }
        public DbSet<BillAddress> BillAddress { get; set; }
        public DbSet<ShipAddress> ShipAddress { get; set; }
        public DbSet<Customer> Customer { get; set; }
        public DbSet<CustomerAddress> CustomerAddress { get; set; }
        public DbSet<Payment> Payment { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(sqlConn);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder
                .Entity<Stock>(eb =>
                {
                    eb.HasNoKey();
                    eb.ToTable("Stock");
                })
                .Entity<Price>(eb =>
                {
                    eb.HasNoKey();
                    eb.ToTable("Price");
                });
        }
    }
}