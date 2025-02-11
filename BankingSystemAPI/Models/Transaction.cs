namespace BankingSystemAPI.Models
{
    public class Transaction
    {
        public int TransactionID { get; set; }
        public decimal Amount { get; set; }
        public string? Status { get; set; }
        public string? TransactionType { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public int AccountID { get; set; }
        public Account Account { get; set; }
        public int UserID { get; set; }
        public User User { get; set; }
    }
}
