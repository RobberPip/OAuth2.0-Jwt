
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Добавление необходимых сервисов
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSwaggerGen(options =>
{
    options.EnableAnnotations();

    // Добавление описания для Swagger
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "DataBases API",
        Version = "v1",
        Description = "API для работы с БД.",
        Contact = new OpenApiContact
        {
            Name = "Мой GitHub",
            Url = new Uri("https://github.com/RobberPip")
        },
    });
});
// Настройка JSON сериализации с использованием Newtonsoft.Json
builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore; // Игнорируем null-значения
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var app = builder.Build();

// Конфигурация HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();