using Api.EventHandlers.Dtos;
using Api.WebSockets;
using Fleck;
using WebSocketBoilerplate;

namespace Api.EventHandlers;

public class ClientEntersLobbyDto : BaseDto;

public class ServerPutsClientInLobbyAndBroadcastsToEveryoneDto : BaseDto
{
    public List<string> AllClientIds { get; set; }
}

public class ClientEntersLobbyEventHandler(IConnectionManager connectionManager)
    : BaseEventHandler<ClientEntersLobbyDto>
{
    public override async Task Handle(ClientEntersLobbyDto dto,
        IWebSocketConnection socket)
    {
        var clientId = await connectionManager.GetClientIdFromSocketId(
            socket.ConnectionInfo.Id.ToString());
        await connectionManager.AddToTopic("lobby", clientId);
        var allClients = await connectionManager.GetMembersFromTopicId("lobby");
        await connectionManager.BroadcastToTopic("lobby",
            new ServerPutsClientInLobbyAndBroadcastsToEveryoneDto { AllClientIds = allClients });
        var confirmationToClient = new ServerConfirmsDto()
        {
            requestId = dto.requestId,
            Success = true
        };
        socket.SendDto(confirmationToClient);
    }
}