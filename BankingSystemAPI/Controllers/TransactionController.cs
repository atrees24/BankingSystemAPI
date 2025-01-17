using System.Transactions;
using BankingSystemAPI.Data;
using Microsoft.AspNetCore.Mvc;

namespace BankingSystemAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionController : ControllerBase
    {
        private readonly BankingContext _context;

        public TransactionController(BankingContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult GetAllTransactions()
        {
            var transactions = _context.Transactions.ToList();
            return Ok(transactions);
        }

        //[HttpPost]
        //public IActionResult CreateTransaction([FromBody] Transaction transaction)
        //{
        //    _context.Transactions.Add(transaction);
        //    _context.SaveChanges();
        //    return CreatedAtAction(nameof(GetAllTransactions), new { id = transaction.TransactionID }, transaction);
        //}
    }
}
