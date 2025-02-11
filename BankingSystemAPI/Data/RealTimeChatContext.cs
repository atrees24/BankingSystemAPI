using BankingSystemAPI.Models.RealTimeChat;
using Microsoft.EntityFrameworkCore;

namespace BankingSystemAPI.Data
{
    public class RealTimeChatContext : DbContext
    {
        public RealTimeChatContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Message> Messages { get; set; }
    }
}
