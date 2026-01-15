namespace ExamResult.BFF.Services;

public class TimeSlotService : ITimeSlotService
{
  public bool IsAllowed(string identityNo)
  {
    // 1. Validasyon: Kimlik No boş mu?
    if (string.IsNullOrEmpty(identityNo) || !long.TryParse(identityNo, out _))
      return false;

    // 2. Son haneyi al
    int lastDigit = int.Parse(identityNo.Substring(identityNo.Length - 1));

    // 3. Şu anki saati al
    var now = DateTime.Now.Hour; // Sadece saati alıyoruz (Örn: 14)

    // MANTIKSAL KURAL SETİ (TRAFFIC SHAPING)
    // Gerçek hayatta burası config dosyasından okunmalıdır.

    // Grup A (Sonu 0, 2): 10:00 - 12:00
    if ((lastDigit == 0 || lastDigit == 2) && (now >= 10 && now < 12))
      return true;

    // Grup B (Sonu 4, 6): 12:00 - 14:00
    if ((lastDigit == 4 || lastDigit == 6) && (now >= 12 && now < 14))
      return true;

    // Grup C (Sonu 8): 14:00 - 16:00
    if ((lastDigit == 8) && (now >= 14 && now < 16))
      return true;

    // Gece testi yapıyorsak (Debug Modu):
    // Eğer saat 18:00'den sonraysa herkese izin ver (Demo yapabilmek için)
    // Test için geçici olarak devre dışı bırakıldı
    // if (now >= 18) return true;

    return false;
  }

  public string GetAllowedTimeRange(string identityNo)
  {
    if (string.IsNullOrEmpty(identityNo)) return "Geçersiz Kimlik";

    int lastDigit = int.Parse(identityNo.Substring(identityNo.Length - 1));

    if (lastDigit == 0 || lastDigit == 2) return "10:00 - 12:00";
    if (lastDigit == 4 || lastDigit == 6) return "12:00 - 14:00";
    if (lastDigit == 8) return "14:00 - 16:00";

    return "Belirlenemedi";
  }
}