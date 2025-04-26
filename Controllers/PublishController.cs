using Microsoft.AspNetCore.Mvc;
using Dapr.Client;

namespace dapr_sample_net_project.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PublishController : ControllerBase
    {
        private readonly DaprClient _daprClient;

        public PublishController(DaprClient daprClient)
        {
            _daprClient = daprClient;
        }

        [HttpPost("order")]
        public async Task<IActionResult> PublishOrder([FromBody] Order order)
        {
            await _daprClient.PublishEventAsync("pubsub", "neworder", order);
            Console.WriteLine("📤 Published order: " + order.Id);
            return Ok(new { message = "Order published successfully!" });
        }
    }

    public class Order
    {
        public string Id { get; set; }
        public string Product { get; set; }
    }
}
