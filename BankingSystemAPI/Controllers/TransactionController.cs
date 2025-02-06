using BankingSystemAPI.Data;
using BankingSystemAPI.Models;
using BankingSystemAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BankingSystemAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionController : ControllerBase
    {
        private readonly BankingContext _context;
        private readonly TaxService _taxService;
        private readonly OTPService _otpService;
        private readonly EmailService _emailService;
        private readonly InterestService _interestService;

        public TransactionController
            (BankingContext context, TaxService taxService, OTPService oTPService, EmailService emailService, InterestService interestService)
        {
            _context = context;
            _taxService = taxService;
            _otpService = oTPService;
            _emailService = emailService;
            _interestService = interestService;
        }


        #region Withdraw
        [HttpPost("withdraw")]
        public IActionResult Withdraw(int accountId, decimal amount)
        {
            var account = _context.Accounts.Find(accountId);
            if (account == null)
                return NotFound("Account not found.");

            if (amount <= 0)
                return BadRequest("Withdrawal amount must be positive.");

            if (account.Balance < amount)
                return BadRequest("Insufficient balance.");

            var otp = _otpService.GenerateOTP();
            account.User.OTP = otp;
            account.User.OTPGeneratedAt = DateTime.Now;

            _context.SaveChanges();

            // Send OTP to user's email
            _emailService.SendEmail(account.User.Email, otp);

            // Record the pending transaction in the database
            var transaction = new Transaction
            {
                AccountID = accountId,
                Amount = amount,
                Status = "Pending", // The transaction status is pending until the OTP is confirmed
                TransactionType = "Withdraw",
                CreatedAt = DateTime.Now
            };

            _context.Transactions.Add(transaction);
            _context.SaveChanges();

            return Ok(new
            {
                Message = "OTP sent. Please confirm your transaction.",
                TransactionId = transaction.TransactionID
            });
        }

        [HttpPost("confirm-withdraw")]
        public IActionResult ConfirmWithdraw(int accountId, decimal amount, string otp)
        {
            var account = _context.Accounts.Include(a => a.User).FirstOrDefault(a => a.Id == accountId);
            if (account == null)
                return NotFound(new { Message = "Account not found." });

            // Validate OTP and its expiration time
            var timeElapsed = DateTime.Now - account.User.OTPGeneratedAt;
            if (account.User.OTP != otp || timeElapsed > TimeSpan.FromMinutes(5))
            {
                return BadRequest(new { Message = "Transaction failed. Incorrect or expired OTP." });
            }

            // Begin a database transaction to ensure consistency
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    // Deduct the amount from the account balance
                    account.Balance -= amount;

                    // Record the successful transaction
                    var completedTransaction = new Transaction
                    {
                        AccountID = accountId,
                        Amount = amount,
                        Status = "Success",
                        TransactionType = "Withdraw",
                        CreatedAt = DateTime.Now
                    };

                    _context.Transactions.Add(completedTransaction);
                    _context.SaveChanges();

                    // Commit the transaction
                    transaction.Commit();

                    return Ok(new
                    {
                        Message = "Transaction successful.",
                        RemainingBalance = account.Balance,
                        TransactionId = completedTransaction.TransactionID
                    });
                }
                catch (Exception ex)
                {
                    // Rollback in case of error
                    transaction.Rollback();
                    return StatusCode(500, new { Message = "Transaction failed. Please try again later.", Error = ex.Message });
                }
            }
        }
        #endregion

        #region Deposit
        [HttpPost("deposit")]
        public IActionResult Deposit(int accountId, decimal amount)
        {
            // Retrieve account and associated user
            var account = _context.Accounts.Include(a => a.User).FirstOrDefault(a => a.Id == accountId);
            if (account == null)
                return NotFound("Account not found.");

            if (amount < 5)
                return BadRequest("Deposit amount must be at least $5.");

            // Generate OTP and Transaction ID
            var otpService = new OTPService();
            var otp = otpService.GenerateOTP();

            // Create and save transaction
            var transaction = new Transaction
            {
                Amount = amount,
                Status = "Pending",
                TransactionType = "Deposit",
                AccountID = account.Id,
                UserID = account.User.UserID,
                CreatedAt = DateTime.Now
            };

            _context.Transactions.Add(transaction);

            // Update user with OTP and timestamp
            account.User.OTP = otp;
            account.User.OTPGeneratedAt = DateTime.Now;
            _context.SaveChanges();

            var transactionId = transaction.TransactionID;

            // Send OTP email
            var emailService = new EmailService();
            emailService.SendEmail(account.User.Email, otp, transactionId);

            return Ok(new
            {
                Message = "OTP and Transaction ID sent to your email. Please confirm your deposit.",
                TransactionId = transactionId
            });
        }

        [HttpPost("confirm-deposit")]
        public IActionResult ConfirmDeposit(int accountId, int transactionId, string otp)
        {
            var account = _context.Accounts.Include(a => a.User)
                                           .FirstOrDefault(a => a.Id == accountId);
            if (account == null)
                return NotFound("Account not found.");

            var user = account.User;
            if (user == null)
                return NotFound("User not associated with this account.");

            // Check if OTP is valid and not expired (valid for 5 minutes)
            if (user.OTP != otp || user.OTPGeneratedAt == null ||
                DateTime.Now > user.OTPGeneratedAt.Value.AddMinutes(5))
            {
                return BadRequest("Invalid or expired OTP.");
            }

            // Find the transaction
            var transaction = _context.Transactions
                                      .FirstOrDefault(t => t.AccountID == accountId &&
                                                           t.TransactionID == transactionId &&
                                                           t.Status == "Pending");
            if (transaction == null)
                return NotFound("Transaction not found or already processed.");

            // Return a response indicating that customer service needs to confirm
            return Ok(new
            {
                Message = "Your deposit is under review by customer service. Please wait for confirmation."
            });
        }

        [HttpPost("CustomerService/confirm-deposit")]
        public IActionResult CustomerServiceConfirmDeposit(int transactionId, string otp)
        {
            var transaction = _context.Transactions.Include(t => t.Account).ThenInclude(a => a.User)
                .FirstOrDefault(t => t.TransactionID == transactionId);
            if (transaction == null)
                return NotFound("Transaction not found.");

            var account = transaction.Account;
            var user = account.User;

            // Validate OTP
            if (user.OTP != otp || user.OTPGeneratedAt == null ||
                DateTime.Now > user.OTPGeneratedAt.Value.AddMinutes(5))
            {
                // If OTP is invalid or expired, delete the transaction
                _context.Transactions.Remove(transaction);
                _context.SaveChanges();

                return BadRequest("Invalid or expired OTP. Transaction has been cancelled.");
            }

            // Complete the transaction
            account.Balance += transaction.Amount;
            transaction.Status = "Success";
            account.LastUpdatedAt = DateTime.Now;

            // Clear OTP after successful confirmation
            user.OTP = null;
            user.OTPGeneratedAt = null;

            _context.SaveChanges();

            return Ok(new
            {
                Message = "Deposit confirmed successfully by customer service.",
                NewBalance = account.Balance
            });
        }
        #endregion

        #region Taxes
        [HttpPost("apply-monthly-tax/{accountId}")]
        public IActionResult ApplyMonthlyTax(int accountId)
        {
            var account = _context.Accounts.FirstOrDefault(a => a.Id == accountId);
            if (account == null)
                return NotFound("Account not found.");

            if (account.Balance < 10000)
            {
                return Ok(new
                {
                    Message = "No tax applied as the account balance is below $10,000.",
                    CurrentBalance = account.Balance
                });
            }

            // Apply the monthly tax
            var newBalance = _taxService.ApplyMonthlyTax(account.Balance);
            account.Balance = newBalance;
            account.LastUpdatedAt = DateTime.Now;

            _context.SaveChanges();

            return Ok(new
            {
                AccountBalanceBeforeTax = account.Balance + _taxService.CalculateMonthlyTax(account.Balance),
                NewBalance = account.Balance,
                MonthlyTaxAmount = _taxService.CalculateMonthlyTax(account.Balance)
            });
        }

        [HttpPost("apply-annual-tax/{accountId}")]
        public IActionResult ApplyAnnualTax(int accountId)
        {
            var account = _context.Accounts.FirstOrDefault(a => a.Id == accountId);
            if (account == null)
                return NotFound("Account not found.");

            if (account.Balance < 10000)
            {
                return Ok(new
                {
                    Message = "No tax applied as the account balance is below $10,000.",
                    CurrentBalance = account.Balance
                });
            }

            // Apply the annual tax
            var newBalance = _taxService.ApplyAnnualTax(account.Balance);
            account.Balance = newBalance;
            account.LastUpdatedAt = DateTime.Now;

            _context.SaveChanges();

            return Ok(new
            {
                AccountBalanceBeforeTax = account.Balance + _taxService.CalculateAnnualTax(account.Balance),
                NewBalance = account.Balance,
                AnnualTaxAmount = _taxService.CalculateAnnualTax(account.Balance)
            });
        }
        #endregion

        #region Interests
        [HttpPost("apply-monthly-interest/{accountId}")]
        public IActionResult ApplyMonthlyInterest(int accountId)
        {
            var account = _context.Accounts.FirstOrDefault(a => a.Id == accountId);
            if (account == null)
                return NotFound("Account not found.");

            account.Balance = _interestService.ApplyMonthlyInterest(account.Balance);
            account.LastUpdatedAt = DateTime.Now;

            _context.SaveChanges();

            return Ok(new
            {
                AccountBalanceBeforeInterest = account.Balance - _interestService.CalculateMonthlyInterest(account.Balance),
                NewBalance = account.Balance,
                MonthlyInterestAmount = _interestService.CalculateMonthlyInterest(account.Balance)
            });
        }

        [HttpPost("apply-annual-interest/{accountId}")]
        public IActionResult ApplyAnnualInterest(int accountId)
        {
            var account = _context.Accounts.FirstOrDefault(a => a.Id == accountId);
            if (account == null)
                return NotFound("Account not found.");

            account.Balance = _interestService.ApplyAnnualInterest(account.Balance);
            account.LastUpdatedAt = DateTime.Now;

            _context.SaveChanges();

            return Ok(new
            {
                AccountBalanceBeforeInterest = account.Balance - _interestService.CalculateAnnualInterest(account.Balance),
                NewBalance = account.Balance,
                AnnualInterestAmount = _interestService.CalculateAnnualInterest(account.Balance)
            });
        }
        #endregion
    }
}
