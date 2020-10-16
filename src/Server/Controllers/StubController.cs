using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace TGH.Server.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class StubController : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            await Task.Delay(200);
            return Ok();
        }
    }
}
