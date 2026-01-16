using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using StackExchange.Redis; // 👈 Redis kütüphanesi

namespace ExamResult.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private IConnection _rabbitConnection;
    private IModel _rabbitChannel;
    private ConnectionMultiplexer _redisConnection; // 👈 Redis Bağlantısı
    private IDatabase _redisDb;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        // 1. RabbitMQ Bağlantısı (IP Sabit)
        var factory = new ConnectionFactory
        {
            HostName = "127.0.0.1",
            DispatchConsumersAsync = true
        };

        _rabbitConnection = factory.CreateConnection();
        _rabbitChannel = _rabbitConnection.CreateModel();

        _rabbitChannel.QueueDeclare(queue: "exam_requests", durable: false, exclusive: false, autoDelete: false, arguments: null);

        // 2. Redis Bağlantısı ⚡
        // Docker'daki Redis 6379 portunda çalışıyor.
        _redisConnection = await ConnectionMultiplexer.ConnectAsync("127.0.0.1:6379");
        _redisDb = _redisConnection.GetDatabase();

        _logger.LogInformation("✅ Worker Başladı: RabbitMQ ve Redis bağlantıları hazır.");

        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = new AsyncEventingBasicConsumer(_rabbitChannel);

        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            try
            {
                var examRequest = JsonSerializer.Deserialize<JsonElement>(message);

                string identityNo = "Unknown";
                if (examRequest.TryGetProperty("IdentityNo", out var idProp))
                    identityNo = idProp.GetString();

                // İŞLEM SİMÜLASYONU
                // Gereksiz logları kaldırdık, sadece işlem bitince haber vereceğiz.
                await Task.Delay(2000, stoppingToken);

                // REDIS'E YAZMA (CACHING)
                // Key: "result:11111" -> Value: "Kazandınız! Puan: 450"
                // TTL: 1 Saat (Data 1 saat sonra silinsin)
                var resultData = JsonSerializer.Serialize(new
                {
                    Score = new Random().Next(300, 500),
                    Status = "Kazandı",
                    GeneratedAt = DateTime.Now
                });

                await _redisDb.StringSetAsync(
                    key: $"result:{identityNo}",
                    value: resultData,
                    expiry: TimeSpan.FromHours(1)
                );

                _logger.LogInformation($"[CACHE] {identityNo} sonucu Redis'e yazıldı.");

                // Kuyruktan düşür
                _rabbitChannel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ERROR] Hata: {ex.Message}");
            }
        };

        _rabbitChannel.BasicConsume(queue: "exam_requests", autoAck: false, consumer: consumer);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override void Dispose()
    {
        _rabbitChannel?.Close();
        _rabbitConnection?.Close();
        _redisConnection?.Close();
        base.Dispose();
    }
}