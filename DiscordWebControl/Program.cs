using DiscordBot;
using DiscordBot.Exceptions;
using DiscordWebControl.Misc;
using Microsoft.AspNetCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddAuthorization();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

#region CorsPolicy

var allowedOrigins = new[]
{
    "https://vlad.mrshurukan.ru",
};


const string myAllowSpecificOrigins = "CorsPolicy";

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: myAllowSpecificOrigins,
        policy =>
        {
            policy.AllowAnyMethod()
                .AllowAnyHeader()
                .WithExposedHeaders("Content-Disposition")
                .AllowCredentials();

            policy.SetIsOriginAllowed(origin =>
            {
                var uri = new Uri(origin).Host;
#if DEBUG
                if (uri is "localhost" or "127.0.0.1")
                    return true;
#endif

                return allowedOrigins.Contains(origin);
            });
        });
});

#endregion

builder.Services.AddControllers();

builder.Services.AddMemoryCache();
builder.Services.AddSingleton<Bot>();
builder.Services.AddSingleton<PlaylistController>();

var app = builder.Build();

app.UseCors(myAllowSpecificOrigins);
        
app.UseExceptionHandler(appBuilder => appBuilder.Run(async context =>
{
    var exceptionHandlerPathFeature =
        context.Features.Get<IExceptionHandlerPathFeature>();

    if (exceptionHandlerPathFeature!.Error is StatusBasedException sbe)
    {
        context.Response.StatusCode = sbe.StatusCode;
    }
    else
    {
        context.Response.StatusCode = 500;
    }

    context.Response.ContentType = "application/json";
    await context.Response.WriteAsJsonAsync(new { error = true, message = exceptionHandlerPathFeature!.Error.Message });
}));

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