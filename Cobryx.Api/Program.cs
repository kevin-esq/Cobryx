using Cobryx.Application;
using Cobryx.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services
    .AddApplicationServices()
    .AddInfrastructureServices(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<Cobryx.Api.Middlewares.GlobalExceptionHandlerMiddleware>();
app.UseMiddleware<Cobryx.Api.Middlewares.TenantMiddleware>();

app.UseHttpsRedirection();

// Basic Health Check (Temporary until)
app.MapGet("/health", () => Results.Ok("Cobryx API is running"));

app.Run();
