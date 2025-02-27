using System.Text.Json;
using Api;
using Api.EventHandlers;
using Api.EventHandlers.Dtos;
using Api.Utilities;
using Api.WebSockets;
using EFScaffold;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using WebSocketBoilerplate;

namespace NUnit;

[TestFixture(typeof(DictionaryConnectionManager))]
public class ConnectionWithWsClient(Type connectionManagerType) : WebApplicationFactory<Program>
{
    private ILogger<ConnectionWithWsClient> _logger;
    private HttpClient _httpClient;
    private KahootContext _dbContext;
    private IConnectionManager _connectionManager;
    private string _wsClientId;
    private WsRequestClient _wsClient;
    private IServiceScope _scope;
    
    [SetUp]
    public async Task Setup()
    {
        _httpClient = CreateClient();

        //Singletons
        _logger = Services.GetRequiredService<ILogger<ConnectionWithWsClient>>();
        _connectionManager = Services.GetRequiredService<IConnectionManager>();

        //Scoped services
        using var scope = Services.CreateScope();
        {
            _scope = Services.CreateScope();
            _dbContext = _scope.ServiceProvider.GetRequiredService<KahootContext>();
        }
        
        var wsPort = Environment.GetEnvironmentVariable("PORT");
        if (string.IsNullOrEmpty(wsPort)) throw new Exception("Environment variable WS_PORT is not set");
        _wsClientId = Guid.NewGuid().ToString();
        var url = "ws://localhost:" + wsPort + "?id=" + _wsClientId;
        _wsClient = new WsRequestClient(
            new[] { typeof(ClientEntersLobbyDto).Assembly },
            url
        );
        await _wsClient.ConnectAsync();
        await Task.Delay(1000);
    }

 


    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.Remove(services.SingleOrDefault(d => d.ServiceType == typeof(IConnectionManager)) ??
                            throw new Exception("Could not find instance of " + nameof(IConnectionManager)));
            services.AddSingleton(typeof(IConnectionManager), connectionManagerType);

            services.Remove(services.SingleOrDefault(d => d.ServiceType == typeof(IGameTimeProvider)) ??
                            throw new Exception("Could not find instance of " + nameof(IGameTimeProvider)));
            var mockGameTimeProvider = new Mock<IGameTimeProvider>();
            mockGameTimeProvider.Setup(s => s.MilliSeconds).Returns(500);
            services.AddSingleton<IGameTimeProvider>(mockGameTimeProvider.Object);
        });
    }


    [Theory]
    public async Task Api_Can_Successfully_Add_Connection()
    {
        var pairForClientId = _connectionManager.GetAllConnectionIdsWithSocketId().Result
            .First(pair => pair.Key == _wsClientId);
        if (pairForClientId.Key != _wsClientId && pairForClientId.Value.Length > 5)
            throw new Exception("ConnectionIdToSocket should have client ID key and a socket ID, but state was: " +
                                "" + JsonSerializer.Serialize(
                                    await _connectionManager.GetAllConnectionIdsWithSocketId()));
        if (_connectionManager.GetAllSocketIdsWithConnectionId().Result.Keys.Count != 1)
            throw new Exception("SocketToConnectionId should have 1 value, but state was: " +
                                "" + JsonSerializer.Serialize(
                                    await _connectionManager.GetAllSocketIdsWithConnectionId()));
    }

    [Theory]
    public async Task ClientCanEnterLobby()
    {
        var dto = new ClientEntersLobbyDto()
        {
            requestId = Guid.NewGuid().ToString()
        };
        var result = await _wsClient
            .SendMessage<ClientEntersLobbyDto, ServerConfirmsDto>(dto); //if there is no expected response, simply use Send(dto)
        if (result.Success == false)
            throw new Exception("The server should send a confirmation to the client joining the room");

        await Task.Delay(1000); //If broadcasting with no response ID, add time delay 
        var broadcastFromServer = _wsClient.GetMessagesOfType<ServerPutsClientInLobbyAndBroadcastsToEveryoneDto>()
            .FirstOrDefault() ?? throw new Exception("Did not receive broadcast from server");
        _logger.LogInformation(JsonSerializer.Serialize(broadcastFromServer));

        if (broadcastFromServer.AllClientIds.Count == 0)
            throw new Exception("The list of clients in lobby should not be empty");

        var memberDictionaryEntry =
            _connectionManager.GetAllMembersWithTopics().Result.First(key => key.Key == _wsClientId);
        if (memberDictionaryEntry.Value.First() != "lobby")
            throw new Exception(
                "Exepected lobby to be in the hashset: " + memberDictionaryEntry.Value.ToList());

        var topicDictionaryEntry = _connectionManager.GetAllTopicsWithMembers().Result.First(key => key.Key == "lobby");
        if (topicDictionaryEntry.Value.First() != _wsClientId)
            throw new Exception("Expected " + _wsClientId + " to be in the hashset: " +
                                memberDictionaryEntry.Value.ToList());
        
    }
}