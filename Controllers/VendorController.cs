using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InframartAPI_New.Controllers
{
    [Route("vendor")]
    [ApiController]
    public class VendorController : ControllerBase
    {
        [Authorize(Roles = "vendor")]
        [HttpGet("dashboard")]
        public IActionResult Dashboard()
        {
            return Ok("Vendor Dashboard Access Granted");
        }

        [Authorize(Roles = "vendor")]
        [HttpGet("profile")]
        public IActionResult Profile()
        {
            return Ok("Vendor Profile Data");
        }
    }
}