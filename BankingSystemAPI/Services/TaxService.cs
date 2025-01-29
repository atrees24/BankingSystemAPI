namespace BankingSystemAPI.Services
{
    public class TaxService
    {
        private const decimal TaxRate = 0.05m;
        private const decimal MinimumBalanceForTax = 10000m;

        public decimal CalculateMonthlyTax(decimal balance)
        {
            if (balance < MinimumBalanceForTax)
                return 0;

            return balance * TaxRate;
        }

        // Calculate annual tax.
        public decimal CalculateAnnualTax(decimal balance)
        {
            if (balance < MinimumBalanceForTax)
                return 0;

            return balance * TaxRate * 12;
        }

        // Apply monthly tax to the balance
        public decimal ApplyMonthlyTax(decimal balance)
        {
            return balance - CalculateMonthlyTax(balance);
        }

        // Apply annual tax to the balance
        public decimal ApplyAnnualTax(decimal balance)
        {
            return balance - CalculateAnnualTax(balance);
        }
    }
}
