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
        public DbSet<ProductImage> ProductImage { get; set; }
        public DbSet<ProductTempImage> ProductTempImage { get; set; }
        public virtual DbSet<Stock> Stock { get; set; }
        public virtual DbSet<Price> Price { get; set; }
        public virtual DbSet<ProductKelly> ProductKelly { get; set; }
        public virtual DbSet<KellyChild> KellyChild { get; set; }
        public virtual DbSet<Filter> Filter { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Item> Item { get; set; }
        public DbSet<BillAddress> BillAddress { get; set; }
        public DbSet<ShipAddress> ShipAddress { get; set; }
        public DbSet<Customer> Customer { get; set; }
        public DbSet<CustomerAddress> CustomerAddress { get; set; }
        public DbSet<Payment> Payment { get; set; }
        public DbSet<Brand> Brand { get; set; }
        public DbSet<ProductType> ProductType { get; set; }
        public virtual DbSet<Sku> Sku { get; set; }

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
                })
                .Entity<ProductKelly>(eb =>
                {
                    eb.HasNoKey();
                    eb.ToTable("ProductKelly");
                })
                .Entity<KellyChild>(eb =>
                {
                    eb.HasNoKey();
                    eb.ToTable("KellyChild");
                })
                .Entity<Filter>(eb =>
                {
                    eb.HasNoKey();
                    eb.ToTable("Filter");
                })
                .Entity<Sku>(eb =>
                {
                    eb.HasNoKey();
                    eb.ToTable("Sku");
                });
        }
    }
}