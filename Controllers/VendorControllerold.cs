using Microsoft.AspNetCore.Mvc;
using InframartAPI.Data;
using InframartAPI.Models;

namespace InframartAPI.Controllers
{
    [ApiController]
    [Route("api/vendor")]
    public class VendorController : ControllerBase
    {
        private readonly AppDbContext _context;

        public VendorController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult GetVendors()
        {
            return Ok(_context.Vendors.ToList());
        }

        [HttpPost]
        public IActionResult CreateVendor(Vendor vendor)
        {
            _context.Vendors.Add(vendor);

            _context.SaveChanges();

            return Ok(vendor);
        }
    }
}