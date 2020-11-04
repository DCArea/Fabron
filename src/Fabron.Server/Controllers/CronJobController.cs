using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Fabron.Server.Controllers
{
    [ApiController]
    [Route("CronJobs")]
    public class CronJobController : ControllerBase
    {
        private readonly IJobManager _jobManager;
        public CronJobController(IJobManager jobManager)
        {
            _jobManager = jobManager;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDetail(string id)
        {
            var job = await _jobManager.GetCronJobDetail(id);
            return Ok(job);
        }
    }
}
