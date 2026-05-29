using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InframartAPI_New.Controllers
{
    [Route("api/user")]
    [ApiController]
    public class UserController : ControllerBase
    {
        [Authorize(Roles = "customer")]
        [HttpGet("dashboard")]
        public IActionResult Dashboard()
        {
            return Ok("User Dashboard Access Granted");
        }

        [Authorize(Roles = "customer")]
        [HttpGet("profile")]
        public IActionResult Profile()
        {
            return Ok("User Profile Data");
        }
    }
}