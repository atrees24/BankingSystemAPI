using BankingSystemAPI.Data;
using BankingSystemAPI.Models;
using BankingSystemAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BankingSystemAPI.Controllers
{
    public class TransactionController : Controller
    {
        private readonly BankingContext _context;

        public TransactionController(BankingContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost("withdraw")]
        public IActionResult Withdraw(int accountId, decimal amount)
        {
            var account = _context.Accounts.Find(accountId);
            if (account == null)
                return NotFound("Account not found.");

            if (account.Balance < amount)
                return BadRequest("Insufficient balance.");

            var otpService = new OTPService();
            var otp = otpService.GenerateOTP();
            account.User.OTP = otp;
            account.User.OTPGeneratedAt = DateTime.Now;
            _context.SaveChanges();

            var emailService = new EmailService();
            emailService.SendEmail(account.User.Email, otp);

            return Ok("OTP sent. Please confirm your transaction.");
        }

        [HttpPost("confirm-withdraw")]
        public IActionResult ConfirmWithdraw(int accountId, decimal amount, string otp)
        {
            // Fetch the account by its ID
            var account = _context.Accounts.Find(accountId);
            if (account == null)
                return NotFound("Account not found.");

            // Validate OTP and its expiration time
            var timeElapsed = DateTime.Now - account.User.OTPGeneratedAt;
            if (account.User.OTP != otp || timeElapsed > TimeSpan.FromMinutes(5))
            {
                return BadRequest("Transaction failed. Incorrect or expired OTP.");
            }

            // Deduct the amount from the account balance
            account.Balance -= amount;

            // Record the transaction
            var transaction = new Transaction
            {
                AccountID = accountId,
                Amount = amount,
                Status = "Success",
                TransactionType = "Withdraw",
                CreatedAt = DateTime.Now
            };
            _context.Transactions.Add(transaction);

            // Save changes to the database
            _context.SaveChanges();

            return Ok($"Transaction successful. Remaining balance: {account.Balance}");
        }

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
            transaction.Status = "Completed";
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
    }
}
