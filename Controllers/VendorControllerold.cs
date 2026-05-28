using Microsoft.AspNetCore.Mvc;
using InframartAPI_New.Data;
using InframartAPI_New.Models;
using Microsoft.EntityFrameworkCore;

namespace InframartAPI_New.Controllers
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

        // GET: api/vendor
        [HttpGet]
        public async Task<IActionResult> GetVendors()
        {
            var vendors = await _context.Vendors.ToListAsync();
            return Ok(vendors);
        }

        // POST: api/vendor
        [HttpPost]
        public async Task<IActionResult> CreateVendor([FromBody] Vendor vendor)
        {
            if (vendor == null)
                return BadRequest("Vendor data is null");

            await _context.Vendors.AddAsync(vendor);
            await _context.SaveChangesAsync();

            return Ok(vendor);
        }
    }
}