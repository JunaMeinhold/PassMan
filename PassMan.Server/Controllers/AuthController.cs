namespace PassMan.Server.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http.HttpResults;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Identity.Web;
    using PassMan.Server.Contexts;

    [ApiController]
    [Route("api/v1/[controller]")]
    public class AuthController : Controller
    {
        private readonly AuthContext context;

        public AuthController(AuthContext context)
        {
            this.context = context;
        }

        [HttpPost("Auth")]
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(400)]
        public IActionResult Auth(string username, string password)
        {
            if (!context.TryAuthorize(username, password, out User? user))
            {
                Response.StatusCode = 400;
                return BadRequest();
            }

            Token token = context.GenerateToken(user);
            return Ok(token.Value);
        }

        [HttpPost("Check")]
        [ProducesResponseType(typeof(string), 200)]
        public IActionResult Check(string token)
        {
            if (context.CheckToken(token))
            {
                return Ok("Success");
            }
            return Ok("Invalid");
        }
    }
}