using BankingSystemAPI.Data;
using BankingSystemAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BankingSystemAPI.Controllers
{
    public class UserController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        private readonly BankingContext _context;

        public UserController(BankingContext context)
        {
            _context = context;
        }
        [HttpPost("create-user")]
        public IActionResult CreateUser([FromBody] User user)
        {
            _context.Users.Add(user);
            _context.SaveChanges();
            return Ok(user);
        }

    }
}
