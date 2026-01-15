using ExamResult.BFF.Services;

namespace ExamResult.BFF.Middlewares;

public class EdgeSecurityMiddleware
{
  private readonly RequestDelegate _next;

  public EdgeSecurityMiddleware(RequestDelegate next)
  {
    _next = next;
  }

  // Middleware her istek geldiğinde burayı çalıştırır.
  public async Task InvokeAsync(HttpContext context, ITimeSlotService timeSlotService)
  {
    // 1. Sadece ilgili endpoint'i kontrol et (Diğer sayfalara karışma)
    if (context.Request.Path.Value?.Contains("/api/result/check-status") == true)
    {
      // URL'den ID'yi ayıkla: /api/result/check-status/12345678900
      var pathSegments = context.Request.Path.Value.Split('/');
      var identityNo = pathSegments.Last(); // En sondaki ID'yi al

      // 2. EDGE KONTROLÜ (The Bouncer)
      // Service'i burada çağırıyoruz. Controller'a hiç girmeden karar veriyoruz.
      if (!timeSlotService.IsAllowed(identityNo))
      {
        var allowedTime = timeSlotService.GetAllowedTimeRange(identityNo);

        context.Response.StatusCode = 429; // Too Many Requests
        context.Response.ContentType = "application/json";

        // Edge seviyesinde reddedildiğine dair log
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[EDGE BLOCKED] ID: {identityNo} - Allowed: {allowedTime}");
        Console.ResetColor();

        await context.Response.WriteAsJsonAsync(new
        {
          Error = "Traffic Control",
          Message = "Edge Katmanı: Şu an sıranız değil.",
          AllowedSlot = allowedTime
        });

        return; // ⛔ DUR! Controller'a gitme, buradan geri dön.
      }
    }

    // 3. Sorun yoksa kapıyı aç, içeri (Controller'a) geçsin.
    await _next(context);
  }
}