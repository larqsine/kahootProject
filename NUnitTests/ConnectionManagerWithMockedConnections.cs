using System.Text.Json;
using Api;
using Api.WebSockets;
using Fleck;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;

namespace NUnit;

[TestFixture(typeof(DictionaryConnectionManager))]
public class ConnectionManagerWithMockedConnections(Type connectionManagerType) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.Remove(services.SingleOrDefault(d => d.ServiceType == typeof(IConnectionManager)) ??
                            throw new Exception("Could not find instance of " + nameof(IConnectionManager)));
            services.AddSingleton(typeof(IConnectionManager), connectionManagerType);
        });
    }

    [Test]
    public async Task OnConnect_Can_Add_Socket_And_Client_To_Storage()
    {
        // arrange
        var manager = Services.GetRequiredService<IConnectionManager>();

        var connectionId = Guid.NewGuid().ToString();
        var socketId = Guid.NewGuid();
        var wsMock = new Mock<IWebSocketConnection>();
        wsMock.SetupGet(ws => ws.ConnectionInfo.Id).Returns(socketId);
        var ws = wsMock.Object;

        // act
        await manager.OnOpen(ws, connectionId);

        // assert
        if (!manager.GetAllConnectionIdsWithSocketId().Result.Values.Contains(ws.ConnectionInfo.Id.ToString()))
            throw new Exception("The dictionary should contain the websocket with guid " + ws.ConnectionInfo.Id +
                                " as the first value");
        if (!manager.GetAllSocketIdsWithConnectionId().Result.Values.Contains(connectionId))
            throw new Exception("The dictionary " + nameof(manager.GetAllSocketIdsWithConnectionId) +
                                " should contain the connectionId with guid " + connectionId +
                                " as the first value");
    }

    [Test]
    public async Task OnClose_Can_Remove_Socket_And_Client_From_Storage()
    {
        // arrange
        var manager = Services.GetRequiredService<IConnectionManager>();

        var connectionId = Guid.NewGuid().ToString();
        var socketId = Guid.NewGuid();
        var wsMock = new Mock<IWebSocketConnection>();
        wsMock.SetupGet(ws => ws.ConnectionInfo.Id).Returns(socketId);
        var ws = wsMock.Object;
        await manager.OnOpen(ws, connectionId);

        // act
        await manager.OnClose(ws, connectionId);

        // assert
        if (manager.GetAllConnectionIdsWithSocketId().Result.Values.Contains(ws.ConnectionInfo.Id.ToString()))
            throw new Exception("The dictionary should not contain the websocket with guid " + ws.ConnectionInfo.Id);
        if (manager.GetAllSocketIdsWithConnectionId().Result.Values.Contains(connectionId))
            throw new Exception("The dictionary should not contain the connectionId with guid " + connectionId);
    }

    [Test]
    public async Task AddToTopic_Correctly_Adds_Member_To_Topic()
    {
        var randomTopic = Guid.NewGuid().ToString();
        var randomUser = Guid.NewGuid().ToString();

        var manager = Services.GetRequiredService<IConnectionManager>();
        await manager.AddToTopic(randomTopic, randomUser);

        var members = await manager.GetMembersFromTopicId(randomTopic);
        if (!members.Contains(randomUser))
            throw new Exception("The topic " + randomTopic + " should contain the user " + randomUser);

        var topics = await manager.GetTopicsFromMemberId(randomUser);
        if (!topics.Contains(randomTopic))
            throw new Exception("The user " + randomUser + " should be in the topic " + randomTopic);
    }

    [Test]
    public async Task RemoveFromTopic_Correctly_Removes_Member_From_Topic()
    {
        var randomTopic = Guid.NewGuid().ToString();
        var randomUser = Guid.NewGuid().ToString();

        var manager = Services.GetRequiredService<IConnectionManager>();
        await manager.AddToTopic(randomTopic, randomUser);
        await manager.RemoveFromTopic(randomTopic, randomUser);

        var members = await manager.GetMembersFromTopicId(randomTopic);
        if (members.Contains(randomUser))
            throw new Exception("The topic " + randomTopic + " should not contain the user " + randomUser);
        var topicsFromMemberId = await manager.GetTopicsFromMemberId(randomUser);
        if (topicsFromMemberId.Contains(randomTopic))
            throw new Exception("The user " + randomUser + " should not be in the topic " + randomTopic);
    }
}