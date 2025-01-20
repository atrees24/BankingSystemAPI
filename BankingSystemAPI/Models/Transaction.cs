namespace BankingSystemAPI.Models
{
    public class Transaction
    {
        public int TransactionID { get; set; }
        public int UserID { get; set; }
        public decimal Amount { get; set; }
        public string? Status { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;
        public User? User { get; set; }
    }
}
