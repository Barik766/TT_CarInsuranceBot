//using Microsoft.AspNetCore.Mvc;

//namespace CarInsuranceBot.Api.Controllers
//{
//    [ApiController]
//    [Route("")]
//    public class HealthController : ControllerBase
//    {
//        private readonly ILogger<HealthController> _logger;

//        public HealthController(ILogger<HealthController> logger)
//        {
//            _logger = logger;
//        }

//        [HttpGet]
//        public IActionResult Get()
//        {
//            _logger.LogInformation("Health check requested");
//            return Ok(new
//            {
//                status = "OK",
//                message = "Car Insurance Bot is running",
//                timestamp = DateTime.UtcNow
//            });
//        }

//        [HttpGet("health")]
//        public IActionResult Health()
//        {
//            return Ok(new
//            {
//                status = "healthy",
//                service = "CarInsuranceBot",
//                timestamp = DateTime.UtcNow
//            });
//        }
//    }
//}