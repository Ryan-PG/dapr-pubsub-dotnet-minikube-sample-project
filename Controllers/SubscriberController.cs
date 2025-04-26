using Dapr;
using Microsoft.AspNetCore.Mvc;

namespace dapr_sample_net_project.Controllers
{
    [ApiController]
    [Route("orders")]
    public class SubscriberController : ControllerBase
    {
        [Topic("pubsub", "neworder")]
        [HttpPost("neworder")]
        public IActionResult HandleNewOrder([FromBody] Order order)
        {
            Console.WriteLine($"📥 Received Order: {order.Id} - {order.Product}");
            return Ok();
        }
    }
}
