namespace BankingSystemAPI.Services
{
    public class InterestService
    {
        private readonly decimal _monthlyInterestRate = 0.05m;
        private readonly decimal _annualInterestRate = 0.10m;

        public decimal CalculateMonthlyInterest(decimal balance)
        {
            if (balance <= 3000)
                return 0;
            else if (balance <= 5000)
                return balance * 0.055m;
            else if (balance <= 100000)
                return balance * 0.085m;
            else if (balance <= 1000000)
                return balance * 0.0875m;
            else
                return balance * 0.09m;
        }
        public decimal CalculateAnnualInterest(decimal balance)
        {
            if (balance <= 3000)
                return 0;
            else if (balance <= 5000)
                return balance * 0.057m;
            else if (balance <= 100000)
                return balance * 0.087m;
            else if (balance <= 1000000)
                return balance * 0.09m;
            else
                return balance * 0.11m;
        }
        public decimal ApplyMonthlyInterest(decimal balance)
        {
            var interest = CalculateMonthlyInterest(balance);
            return balance + interest;
        }
        public decimal ApplyAnnualInterest(decimal balance)
        {
            var interest = CalculateAnnualInterest(balance);
            return balance + interest;
        }
    }
}
