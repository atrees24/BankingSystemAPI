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
        public IActionResult Withdraw(int userId, decimal amount)
        {
            var user = _context.Users.Find(userId);
            if (user == null)
                return NotFound("User not found.");

            if (user.Balance < amount)
                return BadRequest("Insufficient balance.");

           
            var otpService = new OTPService();
            var otp = otpService.GenerateOTP();
            user.OTP = otp;
            user.OTPGeneratedAt = DateTime.Now;
            _context.SaveChanges();

            var emailService = new EmailService();
            emailService.SendOtpEmail(user.Email, otp);

            return Ok("OTP sent. Please confirm your transaction.");
        }

        [HttpPost("confirm-withdraw")]
        public IActionResult ConfirmWithdraw(int userId, decimal amount, string otp)
        {
            var user = _context.Users.Find(userId);
            if (user == null)
                return NotFound("User not found.");

            var timeElapsed = DateTime.Now - user.OTPGeneratedAt;
            if (user.OTP != otp || timeElapsed > TimeSpan.FromMinutes(5))
            {
                return BadRequest("Transaction failed. Incorrect or expired OTP.");
            }

            user.Balance -= amount;
            var transaction = new Transaction
            {
                UserID = userId,
                Amount = amount,
                Status = "Success"
            };
            _context.Transactions.Add(transaction);
            _context.SaveChanges();

            return Ok($"Transaction successful. Remaining balance: {user.Balance}");
        }

    }
}
