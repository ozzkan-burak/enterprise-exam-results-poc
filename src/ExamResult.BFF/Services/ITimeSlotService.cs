namespace ExamResult.BFF.Services;

public interface ITimeSlotService
{
  // Adayın kimlik numarasına göre şu an sorgulama yapıp yapamayacağını döner.
  bool IsAllowed(string identityNo);

  // Eğer izin yoksa, ne zaman gelmesi gerektiğini söyler (Kullanıcı dostu mesaj için).
  string GetAllowedTimeRange(string identityNo);
}