using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace ExamResult.BFF.Services;

public class RabbitMQProducer : IRabbitMQProducer
{
  public void SendMessage<T>(T message)
  {
    // 127.0.0.1: Localhost sorunu yaşamamak için IP kullandık.
    var factory = new ConnectionFactory
    {
      HostName = "127.0.0.1",
      UserName = "guest",
      Password = "guest",
      Port = 5672
    };

    // Bağlantı ve kanal yönetimi
    using var connection = factory.CreateConnection();
    using var channel = connection.CreateModel();

    // Publisher Confirms (Mesajın gittiğinden emin olmak için)
    channel.ConfirmSelect();

    channel.QueueDeclare(queue: "exam_requests",
                         durable: false,
                         exclusive: false,
                         autoDelete: false,
                         arguments: null);

    var json = JsonSerializer.Serialize(message);
    var body = Encoding.UTF8.GetBytes(json);

    var properties = channel.CreateBasicProperties();
    properties.Persistent = false; // Mesajı diske yazma (Hız için)

    channel.BasicPublish(exchange: "",
                         routingKey: "exam_requests",
                         basicProperties: properties,
                         body: body);

    // Mesajın broker'a ulaştığını teyit et (Maks 5 saniye bekle)
    channel.WaitForConfirmsOrDie(new TimeSpan(0, 0, 5));
  }
}