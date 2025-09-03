using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using SeaFight.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddSignalR();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SeaFight API",
        Version = "v1",
        Description = "API для игры Морской Бой",
        Contact = new OpenApiContact
        {
            Name = "Development Team",
            Email = "dev@seafight.com"
        }
    });

});

builder.Services.AddDbContext<SeaFightDbContext>(
    options =>
    {
        options.UseNpgsql(builder.Configuration.GetConnectionString("SeaFightDbContext"));
    });

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowUnoApp", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader()
              .WithOrigins("https://localhost:5000");
    });
});




var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SeaFight API v1");
        c.RoutePrefix = "swagger"; // Доступ по /swagger
    });
}

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}


app.UseCors("AllowAll");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
