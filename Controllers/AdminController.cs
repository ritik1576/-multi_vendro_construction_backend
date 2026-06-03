using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InframartAPI_New.Controllers
{
    [Route("auth/admin")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        [Authorize(Roles = "admin")]
        [HttpGet("dashboard")]
        public IActionResult Dashboard()
        {
            return Ok("Admin Dashboard Access Granted");
        }

        [Authorize(Roles = "admin")]
        [HttpGet("users")]
        public IActionResult GetAllUsers()
        {
            return Ok("Only admin can see all users");
        }
    }
}