using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaymentApi.Data;
using StackExchange.Redis;
using RabbitMQ.Client;
using System.Text;

namespace PaymentApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly PaymentDbContext _context;
        private readonly IConnectionMultiplexer? _redis;
        private readonly IConfiguration _configuration;

        public HealthController(PaymentDbContext context, IConfiguration configuration, IConnectionMultiplexer? redis = null)
        {
            _context = context;
            _configuration = configuration;
            _redis = redis;
        }

        [HttpGet]
        public async Task<IActionResult> GetHealth()
        {
            var healthStatus = new Dictionary<string, object>
            {
                ["status"] = "Healthy",
                ["timestamp"] = DateTime.UtcNow,
                ["services"] = new Dictionary<string, string>()
            };

            var services = (Dictionary<string, string>)healthStatus["services"];

            // Check database
            try
            {
                await _context.Database.CanConnectAsync();
                services["database"] = "Healthy";
            }
            catch (Exception ex)
            {
                services["database"] = $"Unhealthy: {ex.Message}";
                healthStatus["status"] = "Unhealthy";
            }

            // Check Redis
            try
            {
                if (_redis != null)
                {
                    var db = _redis.GetDatabase();
                    await db.PingAsync();
                    services["redis"] = "Healthy";
                }
                else
                {
                    services["redis"] = "Not configured";
                }
            }
            catch (Exception ex)
            {
                services["redis"] = $"Unhealthy: {ex.Message}";
                healthStatus["status"] = "Unhealthy";
            }

            // Check RabbitMQ
            try
            {
                var rabbitMQHost = _configuration["RabbitMQSettings:HostName"];
                var rabbitMQPort = int.Parse(_configuration["RabbitMQSettings:Port"] ?? "5672");
                var rabbitMQUser = _configuration["RabbitMQSettings:UserName"];
                var rabbitMQPass = _configuration["RabbitMQSettings:Password"];

                if (!string.IsNullOrEmpty(rabbitMQHost))
                {
                    var factory = new ConnectionFactory
                    {
                        HostName = rabbitMQHost,
                        Port = rabbitMQPort,
                        UserName = rabbitMQUser,
                        Password = rabbitMQPass
                    };

                    using var connection = factory.CreateConnection();
                    using var channel = connection.CreateModel();
                    services["rabbitmq"] = "Healthy";
                }
                else
                {
                    services["rabbitmq"] = "Not configured";
                }
            }
            catch (Exception ex)
            {
                services["rabbitmq"] = $"Unhealthy: {ex.Message}";
                healthStatus["status"] = "Unhealthy";
            }

            return healthStatus["status"].ToString() == "Healthy" ? Ok(healthStatus) : StatusCode(503, healthStatus);
        }

        [HttpGet("ready")]
        public async Task<IActionResult> GetReadiness()
        {
            try
            {
                // Check if database is ready
                await _context.Database.CanConnectAsync();
                return Ok(new { status = "Ready", timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                return StatusCode(503, new { status = "Not Ready", error = ex.Message, timestamp = DateTime.UtcNow });
            }
        }

        [HttpGet("live")]
        public IActionResult GetLiveness()
        {
            return Ok(new { status = "Alive", timestamp = DateTime.UtcNow });
        }
    }
}
