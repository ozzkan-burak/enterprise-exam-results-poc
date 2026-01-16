using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace ExamResult.BFF.Services;

public class RabbitMQProducer : IRabbitMQProducer
{
  private readonly IConfiguration _configuration;

  public RabbitMQProducer(IConfiguration configuration)
  {
    _configuration = configuration;
  }

  public void SendMessage<T>(T message)
  {
    // 1. Bağlantı Ayarları (Configuration'dan geliyor)
    var factory = new ConnectionFactory
    {
      HostName = _configuration["RabbitMQ:HostName"] ?? "localhost",
      UserName = _configuration["RabbitMQ:UserName"] ?? "guest",
      Password = _configuration["RabbitMQ:Password"] ?? "guest"
    };

    // 2. Bağlantı ve Kanal Oluşturma
    // Not: Performans için normalde bağlantı Singleton tutulur ama POC için using kullanıyoruz.
    using var connection = factory.CreateConnection();
    using var channel = connection.CreateModel();

    // 3. Kuyruğu Tanımla (Yoksa oluşturur)
    var queueName = _configuration["RabbitMQ:QueueName"] ?? "exam_requests";
    channel.QueueDeclare(queue: queueName,
                         durable: false,
                         exclusive: false,
                         autoDelete: false,
                         arguments: null);

    // 4. Mesajı JSON'a çevir
    var json = JsonSerializer.Serialize(message);
    var body = Encoding.UTF8.GetBytes(json);

    // 5. Kuyruğa Fırlat! 🚀
    channel.BasicPublish(exchange: "",
                         routingKey: queueName,
                         basicProperties: null,
                         body: body);
  }
}