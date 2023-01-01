using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GameRoomServer.Shared;

namespace GameRoomsService.GameRoomServer
{
    public class ServerEngine
    {
        public struct Stats
        {
            public NetworkStats NetworkStats;
            public List<int> HangingClients;
        }

        protected virtual uint NetProtocolVersion { get; }

        public event Action<int, string> OnClientWasAuthenticated = null!;
        public event Action<int> OnClientWasDisconnected = null!;

        private readonly ConcurrentDictionary<int, string> _clientId2ProgressId;
        private readonly ConcurrentDictionary<string, int> _progressId2ClientId;

        private readonly INetworkServer _server;
        private readonly IServerMessageProcessor _messageProcessor;
        private readonly IProgressIdFetcher _progressIdFetcher;
        private readonly Telepathy.ILog _log;

        private readonly TimeSpan _statsHangingClient;

        protected ServerEngine(GameRoomsServerSettings gameRoomsSettings,
            IProgressIdFetcher progressIdFetcher,
            IServerMessageProcessor messageProcessor,
            Telepathy.ILog log)
        {
            _log = log;

            _clientId2ProgressId = new ConcurrentDictionary<int, string>();
            _progressId2ClientId = new ConcurrentDictionary<string, int>();

            _statsHangingClient = TimeSpan.FromMinutes(gameRoomsSettings.StatsHangingClientMinutes);

            _server = new NetworkServer(gameRoomsSettings, _log);
            _messageProcessor = messageProcessor;
            _progressIdFetcher = progressIdFetcher;

            _server.OnGetMessage += OnGetMessageHandler;
            _server.OnClientDisconnected += OnClientDisconnectedHandler;
        }

        private void OnGetMessageHandler(BaseMessage message, int connectionId)
        {
            try
            {
                if (message is ConnectQueryMessage connectQueryMessage)
                {
                    //TODO: Check collisions on connect message spam
                    Task.Run(() => Connect(connectionId, connectQueryMessage));
                    return;
                }

                if (!_server.IsConnectionAuthorized(connectionId))
                {
                    _log.LogError($"Message from unauthorized connection {connectionId}. Disconnecting.", connectionId);
                    _server.DisconnectClient(connectionId);
                    return;
                }

                _messageProcessor.ConvertMessage(message, connectionId);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public void ClearNetworkStats()
        {
            _server.ClearStats();
        }

        public async Task Run(CancellationToken cancellationToken)
        {
            InitProtobufSerialization();

            await _server.Run(cancellationToken);
        }

        /// <summary>
        /// Override this to init protobuff serialization structure.
        /// </summary>
        protected virtual void InitProtobufSerialization()
        {
            throw new NotImplementedException(
                "Override this method to init Protobuf serialization. Call ProtoBuffInit.Init() inside\n " +
                "to link your message types to base Message class");
        }

        private void OnClientDisconnectedHandler(int connectionId)
        {
            if (!_clientId2ProgressId.ContainsKey(connectionId))
            {
                _log.LogInfo($"Connection {connectionId} not found", connectionId);
                return;
            }

            OnClientWasDisconnected?.Invoke(connectionId);
            _clientId2ProgressId.TryRemove(connectionId, out var progressId);
            _progressId2ClientId.TryRemove(progressId ?? string.Empty, out connectionId);

            _log.LogInfo($"ProfileProgress {progressId} disconnect from connection {connectionId}", connectionId);
        }

        private async Task<bool> Connect(int connectionId, ConnectQueryMessage connectQueryMessage)
        {
            if (_server.IsConnectionAuthorized(connectionId))
            {
                _log.LogError($"More than one Connect message from connection {connectionId}", connectionId);
                return false;
            }

            _server.ForgetClientAsNonAuth(connectionId);

            var connectMessage = new ConnectMessage { ResponseTo = connectQueryMessage.Id };

            if (connectQueryMessage.Version != NetProtocolVersion)
            {
                SendErrorMessage(connectionId, connectQueryMessage.Id, ErrorCodeValues.VERSION_MISMATCH,
                    "Version mismatch");

                connectMessage.IsOK = false;
                _server.Send(connectionId, connectMessage.SerializeWithDefaultTypeModel());
                _server.Disconnect(connectionId);
                return false;
            }

            var progressId = await _progressIdFetcher.FetchAsync(connectQueryMessage.Token);
            if (string.IsNullOrEmpty(progressId))
            {
                SendErrorMessage(connectionId, connectQueryMessage.Id, ErrorCodeValues.AUTH_FAILED,
                    $"Authentication failed for token {connectQueryMessage.Token}");
                connectMessage.IsOK = false;
                _server.Send(connectionId, connectMessage.SerializeWithDefaultTypeModel());
                _server.Disconnect(connectionId);
                return false;
            }

            if (_progressId2ClientId.TryGetValue(progressId, out var existingConnectionId))
            {
                SendErrorMessage(connectionId, 0, ErrorCodeValues.AUTH_ANOTHER_DEVICE, "Auth from another device");
                _server.Disconnect(existingConnectionId);
                OnClientDisconnectedHandler(existingConnectionId);
            }

            _clientId2ProgressId.TryAdd(connectionId, progressId);
            _progressId2ClientId.TryAdd(progressId, connectionId);

            connectMessage.IsOK = true;
            _server.Send(connectionId, connectMessage.SerializeWithDefaultTypeModel());
            OnClientWasAuthenticated?.Invoke(connectionId, progressId);
            _log.LogInfo($"ProfileProgress {progressId} load to connection {connectionId} by token {connectQueryMessage.Token}", connectionId);
            return true;
        }

        ArraySegment<byte> SerializeMessage<T>(T message) where T : BaseMessage
        {
            return message.SerializeWithDefaultTypeModel();
        }

        public bool Send<T>(int connectionId, T message) where T : BaseMessage
        {
            var data = message.SerializeWithDefaultTypeModel();
            return _server.Send(connectionId, data);
        }

        public bool Send(int connectionId, ArraySegment<byte> data)
        {
            return _server.Send(connectionId, data);
        }

        public int GetSendQueueCount(int connectionId)
        {
            return _server.GetSendQueueCount(connectionId);
        }
        public void DisconnectClient(int connectionId)
        {
            _server.Disconnect(connectionId);
        }

        private void SendErrorMessage(int connectionId, uint id, int errorCode, string errorText = null)
        {
            var errorMessage = new ErrorMessage
            {
                ResponseTo = id,
                Code = errorCode,
                Message = errorText
            };

            _log.LogInfo($"Error message send to client: connectionId {connectionId} text: {errorText}", connectionId);
            _server.Send(connectionId, errorMessage.SerializeWithDefaultTypeModel());
        }

        public void Release()
        {
            OnClientWasAuthenticated = null!;
            OnClientWasDisconnected = null!;
        }

        /// <summary>
        /// Get info about active clients. Do not call too often.
        /// </summary>
        /// <returns>Stats string</returns>
        public Stats GetStats()
        {
            var result = new Stats
            {
                NetworkStats = _server.GetStats(),
                HangingClients = new List<int>()
            };

            var now = DateTime.Now;

            if (result.NetworkStats.Clients != null)
            {
                foreach (var kvp in result.NetworkStats.Clients)
                {
                    var span = now - kvp.Value;
                    if (span > _statsHangingClient)
                    {
                        result.HangingClients.Add(kvp.Key);
                    }
                }
            }

            return result;
        }
    }
}