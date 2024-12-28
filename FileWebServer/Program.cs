using Microsoft.AspNetCore.Http.Features;
using Scalar.AspNetCore;

namespace FileWebServer;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddAuthorization();

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        
        builder.Services.Configure<FormOptions>(options =>
        {
            options.MultipartBodyLengthLimit = 100 * 1024 * 1024; // 100 MB
        });

        builder.Services.AddControllers();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            // app.UseSwagger(options =>
            // {
            //     options.RouteTemplate = "/openapi/{documentName}.json";
            // });
            // app.MapScalarApiReference(options => 
            // {
            //     options.WithTheme(ScalarTheme.BluePlanet);
            // });

            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.MapControllers();
        app.UseAuthorization();

        app.Run();
    }
}