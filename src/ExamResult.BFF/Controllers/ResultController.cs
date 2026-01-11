using Microsoft.AspNetCore.Mvc;
using ExamResult.BFF.Services;

namespace ExamResult.BFF.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ResultController : ControllerBase
{
  private readonly ITimeSlotService _timeSlotService;

  public ResultController(ITimeSlotService timeSlotService)
  {
    _timeSlotService = timeSlotService;
  }

  [HttpGet("check-status/{identityNo}")]
  public IActionResult CheckStatus(string identityNo)
  {
    // 1. MİMARİ KURAL: Önce Time Slot kontrolü yapılır.
    // Eğer sırası değilse, DB veya Redis'e HİÇ GİDİLMEZ.
    if (!_timeSlotService.IsAllowed(identityNo))
    {
      var allowedTime = _timeSlotService.GetAllowedTimeRange(identityNo);

      return StatusCode(429, new
      {
        Message = "Şu an sorgulama sırasınız gelmemiştir.",
        AllowedTimeRange = allowedTime,
        YourLastDigit = identityNo.Substring(identityNo.Length - 1),
        Status = "BLOCKED_BY_GATEKEEPER"
      });
    }

    // 2. Eğer buraya geldiyse, aday içeri girebilir!
    // (Sonraki adımda buraya RabbitMQ kuyruklama kodunu yazacağız)

    return Ok(new
    {
      Message = "Sıranız uygun, sonucunuz hazırlanıyor...",
      Status = "QUEUED" // Şimdilik mock cevap
    });
  }
}