using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Api.Documentation;
using Api.Utilities;
using Api.WebSockets;
using EFScaffold;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;
using WebSocketBoilerplate;

namespace Api;

public class Program
{
    public static void Main()
    {
        var builder = WebApplication.CreateBuilder();

        builder.Services.AddOptionsWithValidateOnStart<AppOptions>()
            .Bind(builder.Configuration.GetSection(nameof(AppOptions)));

        var appOptions = builder.Services.AddAppOptions();

        builder.Services.AddDbContext<KahootContext>(options =>
        {
            options.UseNpgsql(appOptions.DbConnectionString);
            options.EnableSensitiveDataLogging();
        });
        builder.Services.AddScoped<Seeder>();


        builder.Services.AddSingleton<IGameTimeProvider, GameTimeProvider>();
        builder.Services.AddSingleton<IConnectionManager, DictionaryConnectionManager>();
        builder.Services.AddSingleton<CustomWebSocketServer>();
        builder.Services.InjectEventHandlers(Assembly.GetExecutingAssembly());
        
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddOpenApiDocument(conf =>
        {
            conf.DocumentProcessors.Add(new AddAllDerivedTypesProcessor());
            conf.DocumentProcessors.Add(new AddStringConstantsProcessor());
        });
        
        var app = builder.Build();
        app.UseOpenApi();
        
        app.GenerateTypeScriptClient("/../client/src/generated-client.ts").GetAwaiter().GetResult();
        app.Services.GetRequiredService<CustomWebSocketServer>().Start(app);
        app.Urls.Clear();
        app.Urls.Add("http://*:5000"); //making sure the web api doesnt take up port 8080 which is used by the websocket server

        app.Run();
    }
}