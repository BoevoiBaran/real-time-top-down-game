using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using GameRoomServer.Shared;
using GameRoomsService.Services;
using Microsoft.Extensions.Logging;
using Shared.Network;
using Telepathy;

namespace GameRoomsService.GameRoomServer
{
    public class GameRoomsServer : IServerMessageProcessor, ILog
    {
        private const int SAFE_DISCONNECT_WAITING_PERIOD_IN_MS = 3000;

        private readonly ILogger<GameRoomsServer> _logger;
        private readonly GameRoomsServerSettings _roomsServerSettings;
        private GameRoomsNetworkServer _server;
        private RoomsHolder _roomsHolder;
        private IGameRoomsServerListener _listener;
        
        public GameRoomsServer(RoomsHolder roomsHolder, GameRoomsServerSettings settings, ILogger<GameRoomsServer> logger)
        {
            _roomsServerSettings = settings;
            _logger = logger;
            _roomsHolder = roomsHolder;
        }

        public Task<bool> Initialize()
        {
            _listener = new GameRoomsServerListener();
            _server = new GameRoomsNetworkServer(_roomsServerSettings, _listener, this, this);
            
            _server.OnClientWasAuthenticated += OnClientConnectedHandler;
            _server.OnClientWasDisconnected += OnClientDisconnectedHandler;

            return Task.FromResult(true);
        }

        private async Task DisconnectClient(int connectionId)
        {
            var sw = new Stopwatch();
            sw.Start();
            
            while(_server.GetSendQueueCount(connectionId) > 0)
            {
                if (SendingTimeoutExpired(sw.ElapsedMilliseconds))
                {
                    break;        
                }
                
                await Task.Delay(_roomsServerSettings.TickPeriod);
            }
            
            _server.DisconnectClient(connectionId);
        }

        private bool SendingTimeoutExpired(long timeOut)
        {
            return timeOut >= SAFE_DISCONNECT_WAITING_PERIOD_IN_MS;
        }
        
        public async Task Run(CancellationToken cancellationToken)
        {
            await _server.Run(cancellationToken);
        }
        
        private void OnClientConnectedHandler(int connectionId, string clientId)
        {
            Task.Run(() => ProcessConnection(connectionId, clientId));
        }
        
        private void OnClientDisconnectedHandler(int connectionId)
        {
            ProcessDisconnection(connectionId);
        }
        
        private async Task ProcessConnection(int connectionId, string clientId)
        {
            try
            {
                var roomId = "empty_room_id";
                _roomsHolder.AddClient(connectionId, clientId, roomId);
                
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[ProcessConnection] Failed to process connection for client:[{ClientId}] connection ID:{ConnectionId}",clientId, connectionId);
            }
        }
        
        private void ProcessDisconnection(int connectionId)
        {
            try
            {
                if (!_roomsHolder.TryGetClientId(connectionId, out var clientId))
                {
                    _logger.LogWarning("[ProcessDisconnection] Can't find ClientId for connection ID:{ConnectionId}", connectionId);
                    return;
                }

                _roomsHolder.RemoveClient(connectionId);

                _logger.LogInformation("[ProcessDisconnection] Client:[{ClientId}] disconnected and released connection ID:{ConnectionId}", clientId, connectionId);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[ProcessDisconnection] Failed to process disconnection for connection ID:{ConnectionId}", connectionId);
            }
        }

        Task IServerMessageProcessor.ConvertMessage(BaseMessage message, int connectionId)
        {
            Task.Run(() => ProcessIncomingNetworkMessage(message, connectionId));
            return Task.CompletedTask;
        }
        
        private async Task ProcessIncomingNetworkMessage(BaseMessage message, int connectionId)
        {
            try
            {
                if (!_roomsHolder.TryGetClientId(connectionId, out var clientId))
                {
                    _logger.LogError("[ProcessIncomingNetworkMessage] Can't find ClientId for connection ID:{ConnectionId}", connectionId);
                    return;
                }
                
                switch (message)
                {
                    case GameRoomNetMessage incomingMessage:
                        //TODO Incoming messages process logic
                        break;
                    default:
                        _logger.LogError("[ProcessIncomingNetworkMessage] Unknown message type:[{Type}]", message.GetType());
                        break;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[ProcessIncomingNetworkMessage] Process incoming message for connection ID:{ConnectionId} failed", connectionId);
            }
            
            await Task.FromResult(true);
        }
        
        public void SendMessageToPlayer(int connectionId)
        {
            _server.Send(connectionId, ArraySegment<byte>.Empty);
        }
        
        private object? GetPlayerId(int connectionId)
        {
            throw new NotImplementedException();
        }

        #region Log
        
        public void LogInfo(string message)
        {
            _logger.LogInformation("[GameRoomsServer] {Message}", message);
        }

        public void LogInfo(string message, int connectionId)
        {
            _logger.LogInformation("[GameRoomsServer] {Message} playerId:[{PlayerId}]", message, GetPlayerId(connectionId));
        }

        public void LogWarning(string message)
        {
            _logger.LogWarning("[GameRoomsServer] {Message}", message);
        }

        public void LogWarning(string message, int connectionId)
        {
            _logger.LogWarning("[GameRoomsServer] {Message} playerId:[{PlayerId}]", message, GetPlayerId(connectionId));
        }

        public void LogError(string message)
        {
            _logger.LogError("[GameRoomsServer] {Message}", message);
        }

        public void LogError(string message, int connectionId)
        {
            _logger.LogError("[GameRoomsServer] {Message} playerId:[{PlayerId}]", message, GetPlayerId(connectionId));
        }

        #endregion
    }
}