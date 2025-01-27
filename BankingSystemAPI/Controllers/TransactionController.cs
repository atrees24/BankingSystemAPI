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
            emailService.SendOtpEmail(account.User.Email, otp);

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
            var account = _context.Accounts.Include(a => a.User).FirstOrDefault(a => a.Id == accountId);
            if (account == null)
                return NotFound("Account not found.");

            if (amount <= 5)
                return BadRequest("Deposit amount must be at least $5.");

            // Generate OTP and store it with the account's associated user
            var otpService = new OTPService();
            var otp = otpService.GenerateOTP();
            account.User.OTP = otp;
            account.User.OTPGeneratedAt = DateTime.Now;
            _context.SaveChanges();

            // Send OTP to the user's email
            var emailService = new EmailService();
            emailService.SendOtpEmail(account.User.Email, otp);

            return Ok("OTP sent. Please confirm your deposit.");
        }

        [HttpPost("confirm-deposit")]
        public IActionResult ConfirmDeposit(int accountId, decimal amount, string otp)
        {
            // Find the account by AccountID
            var account = _context.Accounts.Include(a => a.User) // Include the User to access the user's OTP and email
                                           .FirstOrDefault(a => a.Id == accountId);
            if (account == null)
                return NotFound("Account not found.");

            var user = account.User; // Access the associated user

            // Validate the OTP and check if it has expired
            var timeElapsed = DateTime.Now - user.OTPGeneratedAt;
            if (user.OTP != otp || timeElapsed > TimeSpan.FromMinutes(5))
            {
                return BadRequest("Deposit failed. Incorrect or expired OTP.");
            }

            // Deposit amount and update account balance
            account.Balance += amount;

            // Create a transaction record
            var transaction = new Transaction
            {
                AccountID = accountId, // Set the Account ID
                UserID = user.UserID,  // Link to the User
                Amount = amount,
                Status = "Success",
                TransactionType = "Deposit",
                CreatedAt = DateTime.Now
            };

            _context.Transactions.Add(transaction);
            _context.SaveChanges();

            return Ok($"Deposit successful. New balance: {account.Balance}");
        }
    }
}
