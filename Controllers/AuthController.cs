using Microsoft.AspNetCore.Mvc;
using InframartAPI.Data;
using InframartAPI.Models;

namespace InframartAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AuthController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] Vendor vendor)
        {
            _context.Vendors.Add(vendor);
            _context.SaveChanges();

            return Ok(new
            {
                message = "Vendor registered successfully",
                data = vendor
            });
        }
    }
}