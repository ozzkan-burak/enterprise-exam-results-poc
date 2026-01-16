using ExamResult.BFF.Services;
using StackExchange.Redis;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<ITimeSlotService, TimeSlotService>();
builder.Services.AddScoped<IRabbitMQProducer, RabbitMQProducer>();

// 👇 REDIS BAĞLANTISI (Singleton olarak eklenir)
// 127.0.0.1: Localhost IP'si
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect("127.0.0.1:6379"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseMiddleware<ExamResult.BFF.Middlewares.EdgeSecurityMiddleware>();

//app.UseHttpsRedirection();

app.MapControllers();

app.Run();
