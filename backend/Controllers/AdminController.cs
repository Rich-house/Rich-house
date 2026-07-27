using Marketify.Roles;
using Marketify.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marketify.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AdminController(IAuthService authService) 
        {
            _authService = authService;
        }
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = AppRoles.SuperAdmin)]
        [HttpGet("GetAllUsers")]
        public async Task <IActionResult>GetUsers()
        {
            var result  = await _authService.GetAllUsers();
            if(result ==null)
                    return BadRequest("SomeThingError");
              
            
            return Ok(result);
        }
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = AppRoles.SuperAdmin)]
        [HttpGet("GetUserByEmail")]
        public async Task<IActionResult> GetUserByEmail([FromQuery] string email)
        {
            if (email is null)
                return BadRequest("Email Format Wrong");
            var result =await _authService.GetUserByEmail(email);
            if(result == null)
                return BadRequest("Email Format Wrong");

            return Ok(result);  
        }
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = AppRoles.SuperAdmin)]
        [HttpDelete("DeleteUserByEmail")]
        public async Task<IActionResult> DeleteUserByEmail([FromQuery] string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest("Email parameter is required.");

            var isDeleted = await _authService.DeleteUserByEmailAsync(email);
            if (!isDeleted)
                return NotFound($"No user found with the email: {email}, or deletion failed.");

            return Ok(new { Message = $"User with email '{email}' has been deleted successfully." });
        }
    }
} 
