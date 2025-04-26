using Microsoft.AspNetCore.Mvc;

namespace dapr_sample_net_project.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TestController : ControllerBase
    {
        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok("✅ Pong from Dapr-enabled API!");
        }

        [HttpPost("echo")]
        public IActionResult Echo([FromBody] object data)
        {
            Console.WriteLine($"[Echo] Received: {data}");
            return Ok(new { received = data });
        }
    }
}
