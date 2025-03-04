using Api;
using EFScaffold;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// No need for controller services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// SignalR configuration with JSON cycle handling
builder.Services.AddSignalR(options => {
    options.HandshakeTimeout = TimeSpan.FromSeconds(30);
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.EnableDetailedErrors = true;
}).AddJsonProtocol(options => {
    options.PayloadSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve;
    options.PayloadSerializerOptions.MaxDepth = 128; // Increase from default 64
});

// CORS is still needed
builder.Services.AddCors(options => {
    options.AddDefaultPolicy(builder => {
        builder.AllowAnyHeader()
            .AllowAnyMethod()
            .SetIsOriginAllowed(_ => true)
            .AllowCredentials();
    });
});

// Database and services
builder.Services.AddDbContext<KahootContext>(options =>
    options.UseNpgsql(builder.Configuration["AppOptions:DbConnectionString"]));
builder.Services.AddScoped<GameService>();

var app = builder.Build();

// Middleware
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(120),
});

app.UseRouting();
app.UseCors();

// Only map the hub, no controllers
app.UseEndpoints(endpoints =>
{
    endpoints.MapHub<GameHub>("/gamehub");
});

app.Run();