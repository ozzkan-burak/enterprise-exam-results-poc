namespace ExamResult.BFF.Services;

public interface IRabbitMQProducer
{
  // T tipindeki herhangi bir nesneyi kuyruğa atar
  void SendMessage<T>(T message);
}