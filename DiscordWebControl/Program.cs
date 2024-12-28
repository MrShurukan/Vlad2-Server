using DiscordBot;
using DiscordWebControl.Misc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddAuthorization();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

builder.Services.AddMemoryCache();
builder.Services.AddSingleton<Bot>();
builder.Services.AddSingleton<PlaylistController>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

if (app.Configuration.GetValue<bool>("LaunchDiscordBot", true))
{
    Console.WriteLine("Пробую запустить дискорд бота");
#pragma warning disable CS4014
    app.Services.GetService<Bot>()!.StartAsync();
    app.Services.GetService<PlaylistController>();
#pragma warning restore CS4014
}
else
{
    Console.WriteLine("Внимание! Дискорд бот не будет запущен, так как стоит LaunchDiscordBot = false в appsettings.json");    
}

Console.WriteLine("Проверяю зависимости...");
DependencyHelper.TestDependencies();

app.Run();