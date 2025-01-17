using BankingSystemAPI.Models;
using Microsoft.EntityFrameworkCore;


namespace BankingSystemAPI.Data
{
    public class BankingContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Transaction> Transactions { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("Server=.;Database=BankingSystemDB;Trusted_Connection=True;TrustServerCertificate=True");
        }
    }
}
