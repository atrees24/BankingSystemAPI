namespace BankingSystemAPI.Models
{
    public class User
    {
        public int UserID { get; set; }
        public string Name { get; set; } = string.Empty; 
        public string Email { get; set; } = string.Empty;
        public string? OTP { get; set; }
        public DateTime? OTPGeneratedAt { get; set; }
        public Account Account { get; set; }
        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}
