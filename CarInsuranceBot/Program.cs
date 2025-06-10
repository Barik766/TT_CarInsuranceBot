using CarInsuranceBot.Api.Middleware;
using CarInsuranceBot.Application.StateMachine.States;
using CarInsuranceBot.Application.StateMachine;
using CarInsuranceBot.Core.Interfaces.Repositories;
using CarInsuranceBot.Core.Interfaces.Services;
using CarInsuranceBot.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using CarInsuranceBot.Infrastructure.Data;
using Serilog;
using CarInsuranceBot.Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;
using CarInsuranceBot.Application.Handlers;
using CarInsuranceBot.Application.StateMachine.Transitions;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers().AddNewtonsoftJson();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Logging settings
builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .WriteTo.Console()
        .WriteTo.File("logs/bot-.log", rollingInterval: RollingInterval.Day);
});

builder.Services.AddMemoryCache();

// HttpClient
builder.Services.AddHttpClient();

// Core services
builder.Services.AddScoped<ITelegramService, TelegramService>();
builder.Services.AddScoped<IMindeeService, MindeeService>();
builder.Services.AddScoped<IOpenAIService, OpenAIService>(); 
builder.Services.AddScoped<IStateManagerService, StateManagerService>();
builder.Services.AddScoped<IPolicyGeneratorService, PolicyGeneratorService>();

// State Machine
builder.Services.AddScoped<BotStateMachine>();
builder.Services.AddScoped<StartState>();
builder.Services.AddScoped<WaitingPassportState>();
builder.Services.AddScoped<WaitingCarDocState>();
builder.Services.AddScoped<ConfirmationState>();
builder.Services.AddScoped<PriceConfirmationState>();
builder.Services.AddScoped<CompletedState>();
builder.Services.AddScoped<IGlobalCommandHandler, ResetCommandHandler>();
builder.Services.AddScoped<IGlobalCommandHandler, OpenAIQuestionHandler>();

// Database
builder.Services.AddDbContext<BotDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<ISessionRepository, SessionRepository>();

var app = builder.Build();

// Middleware
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<ErrorHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/", () => Results.Ok("CarInsuranceBot is running."));

// Database initialization
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<BotDbContext>();
    context.Database.EnsureCreated();
}

app.Run();