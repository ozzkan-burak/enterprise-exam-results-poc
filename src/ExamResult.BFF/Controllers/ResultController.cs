using Microsoft.AspNetCore.Mvc;
using ExamResult.BFF.Services;
using StackExchange.Redis; // 👈 Redis kütüphanesi
using System.Text.Json; // 👈 JSON işlemleri için

namespace ExamResult.BFF.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ResultController : ControllerBase
{
  private readonly ITimeSlotService _timeSlotService;
  private readonly IRabbitMQProducer _producer;
  private readonly IDatabase _redisDb; // Redis Veritabanı arayüzü

  // Constructor'a Redis bağlantısını (IConnectionMultiplexer) ekledik
  public ResultController(
      ITimeSlotService timeSlotService,
      IRabbitMQProducer producer,
      IConnectionMultiplexer redisConnection)
  {
    _timeSlotService = timeSlotService;
    _producer = producer;
    _redisDb = redisConnection.GetDatabase(); // DB'yi al
  }

  [HttpGet("check-status/{identityNo}")]
  public async Task<IActionResult> CheckStatus(string identityNo)
  {
    // 1. ⚡ REDIS KONTROLÜ (Cache-Aside Pattern)
    // Worker ile aynı key formatını kullanmalıyız: "result:{id}"
    var cacheKey = $"result:{identityNo}";
    var cachedResult = await _redisDb.StringGetAsync(cacheKey);

    if (!cachedResult.IsNullOrEmpty)
    {
      // Varsa hemen döndür! Kuyruğa gitme.
      // Redis'ten gelen string JSON'u objeye çevirip dönebiliriz veya direkt string basabiliriz.
      return Ok(new
      {
        Source = "Redis Cache ⚡", // Hızın kanıtı
        Data = JsonSerializer.Deserialize<object>(cachedResult.ToString())
      });
    }

    // 🛑 (Opsiyonel) Time Slot Kontrolünü buraya koyabiliriz.
    // Cache'te varsa saat kontrolüne takılmasın diyorsan bu if'i yukarıdaki Redis kontrolünden sonraya koy.
    // "Cache yoksa ve saati gelmediyse reddet" mantığı:
    // if (!_timeSlotService.IsAllowed(identityNo)) return StatusCode(429...);


    // 2. 🐢 KUYRUĞA ATMA (Cache Miss)
    var examRequest = new
    {
      IdentityNo = identityNo,
      RequestTime = DateTime.Now,
      IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown"
    };

    _producer.SendMessage(examRequest);

    return Ok(new
    {
      Message = "Sonuç henüz hazır değil, talebiniz kuyruğa alındı.",
      Status = "QUEUED",
      Source = "RabbitMQ 🐇"
    });
  }
}