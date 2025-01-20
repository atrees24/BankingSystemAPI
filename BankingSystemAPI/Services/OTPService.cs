namespace BankingSystemAPI.Services
{
    public class OTPService
    {
        public string GenerateOTP()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString(); 
        }
    }
}
