using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using GameRoomServer.Shared;
using ProtoBuf.Meta;

namespace GameRoomsService.GameRoomServer
{
    public class NetworkServer : INetworkServer
    {
        public event Action<BaseMessage, int> OnGetMessage;
        public event Action<int> OnClientDisconnected;

        private readonly ConcurrentDictionary<int, DateTime> _clientId2LoginTime;

        //HACK: There is no ConcurrentHashSet<T>, and ConcurrentBag<T> is not suitable
        private readonly ConcurrentDictionary<int, DateTime> _clients;

        private readonly GameRoomsServerSettings _settings;
        private readonly int _maxMessagesPerTick;
        private readonly int _pingPeriod;
        private readonly int _tickPeriod;

        private readonly Telepathy.ILog _log;
        private Telepathy.Server _server;

        // Stats
        private readonly object _statsLock;
        private DateTime _startTime;
        private DateTime _statsTime;
        private int _messagesReceived;
        private int _messagesReceivedDelta;
        private int _messagesSent;
        private int _messagesSentDelta;
        private long _bytesReceived;
        private long _bytesReceivedDelta;
        private long _bytesSent;
        private long _bytesSentDelta;

        public NetworkServer(GameRoomsServerSettings settings, Telepathy.ILog log)
        {
            _settings = settings;
            _log = log;
            _statsLock = new object();

            _clientId2LoginTime = new ConcurrentDictionary<int, DateTime>();
            _clients = new ConcurrentDictionary<int, DateTime>();

            _maxMessagesPerTick = settings.MaxMessagesPerTick;
            _pingPeriod = settings.PingPeriod;
            _tickPeriod = settings.TickPeriod;
        }

        public async Task Run(CancellationToken cancellationToken)
        {
            ClearStats();

            try
            {
                if (_server == null)
                {
                    _server = new Telepathy.Server(_settings.MaxMessageSize, _log);
                    _server.OnConnected += OnConnectedHandler;
                    _server.OnData += OnDataHandler;
                    _server.OnDisconnected += OnDisconnectedHandler;
                }

                _server.Start(_settings.Port);
            }
            catch (Exception e)
            {
                LogException(e, "FATAL: Telepathy Start Failed");
                throw;
            }

            var pingWatch = new Stopwatch();
            var usePing = _pingPeriod > 0;
            if (usePing)
            {
                pingWatch.Start();
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                _server.Tick(_maxMessagesPerTick);

                if (usePing)
                {
                    Ping(pingWatch);
                }

                await Task.Delay(_tickPeriod, cancellationToken);
            }

            _server.Stop();
        }

        private void OnConnectedHandler(int connectionId)
        {
            var time = DateTime.Now;
            _clients.TryAdd(connectionId, time);
            _clientId2LoginTime.TryAdd(connectionId, time);

            Interlocked.Increment(ref _messagesReceived);
            Interlocked.Increment(ref _messagesReceivedDelta);
        }

        private void OnDataHandler(int connectionId, ArraySegment<byte> data)
        {
            var size = 0;
            BaseMessage message = null;

            try
            {
                size = data.Count;
                message = data.Deserialize(RuntimeTypeModel.Default);
            }
            catch (Exception e)
            {
                LogException(e, $"Failed to process message from connection {connectionId}");
                LogDataError(data);
            }

            if (message != null)
            {
                OnGetMessage?.Invoke(message, connectionId);
                _clients[connectionId] = DateTime.Now;
            }

            Interlocked.Increment(ref _messagesReceived);
            Interlocked.Increment(ref _messagesReceivedDelta);
            Interlocked.Add(ref _bytesReceived, size);
            Interlocked.Add(ref _bytesReceivedDelta, size);
        }

        private void OnDisconnectedHandler(int connectionId)
        {
            OnClientDisconnected?.Invoke(connectionId);
            _clients.TryRemove(connectionId, out _);
            _clientId2LoginTime.TryRemove(connectionId, out _);

            Interlocked.Increment(ref _messagesReceived);
            Interlocked.Increment(ref _messagesReceivedDelta);
        }

        private void Ping(Stopwatch pingWatch)
        {
            if (pingWatch.ElapsedMilliseconds < _pingPeriod)
                return;

            var pingMessage = new NetworkPingMessage {Tick = DateTime.Now.Ticks};
            foreach (var connectionId in _clients.Keys)
            {
                Send(connectionId, pingMessage.SerializeWithDefaultTypeModel());
            }

            pingWatch.Restart();
        }

        public bool Send(int connectionId, ArraySegment<byte> data)
        {
            var size = 0;
            var result = false;

            try
            {
                size = data.Count;
                result = _server.Send(connectionId, data);
            }
            catch (Exception e)
            {
                LogException(e, $"Failed to send message to connection {connectionId}");
            }

            if (result)
            {
                Interlocked.Increment(ref _messagesSent);
                Interlocked.Increment(ref _messagesSentDelta);
                Interlocked.Add(ref _bytesSent, size);
                Interlocked.Add(ref _bytesSentDelta, size);
                _clients[connectionId] = DateTime.Now;
            }

            return result;
        }

        public int GetSendQueueCount(int connectionId)
        {
            return _server.GetSendPipeCount(connectionId);
        }

        public bool DisconnectClient(int connectionId)
        {
            var result = _server.Disconnect(connectionId);
            if (result)
            {
                _clients.TryRemove(connectionId, out _);
                _clientId2LoginTime.TryRemove(connectionId, out _);
            }

            return result;
        }

        public bool Disconnect(int connectionId)
        {
            var result = _server.Disconnect(connectionId);
            return result;
        }

        public void ForgetClientAsNonAuth(int connectionId)
        {
            _clientId2LoginTime.TryRemove(connectionId, out _);
        }

        public bool IsConnectionAuthorized(int connectionId)
        {
            return !_clientId2LoginTime.ContainsKey(connectionId);
        }

        public NetworkStats GetStats()
        {
            TimeSpan runtime;
            TimeSpan runtimeDelta;

            lock (_statsLock)
            {
                var now = DateTime.Now;
                runtime = now - _startTime;
                runtimeDelta = now - _statsTime;
                _statsTime = now;
            }

            var result = new NetworkStats
            {
                Runtime = runtime,
                RuntimeDelta = runtimeDelta,

                MessagesReceived = _messagesReceived,
                MessagesReceivedDelta = _messagesReceivedDelta,
                MessagesSent = _messagesSent,
                MessagesSentDelta = _messagesSentDelta,

                MessagesInQueue = _server?.ReceivePipeTotalCount ?? 0,

                BytesReceived = _bytesReceived,
                BytesReceivedDelta = _bytesReceivedDelta,
                BytesSent = _bytesSent,
                BytesSentDelta = _bytesSentDelta
            };

            Interlocked.Exchange(ref _messagesReceivedDelta, 0);
            Interlocked.Exchange(ref _messagesSentDelta, 0);
            Interlocked.Exchange(ref _bytesReceivedDelta, 0L);
            Interlocked.Exchange(ref _bytesSentDelta, 0L);

            return result;
        }

        public void ClearStats()
        {
            lock (_statsLock)
            {
                var now = DateTime.Now;
                _startTime = now;
                _statsTime = now;
            }

            Interlocked.Exchange(ref _messagesReceived, 0);
            Interlocked.Exchange(ref _messagesReceivedDelta, 0);
            Interlocked.Exchange(ref _messagesSent, 0);
            Interlocked.Exchange(ref _messagesSentDelta, 0);

            Interlocked.Exchange(ref _bytesReceived, 0L);
            Interlocked.Exchange(ref _bytesReceivedDelta, 0L);
            Interlocked.Exchange(ref _bytesSent, 0L);
            Interlocked.Exchange(ref _bytesSentDelta, 0L);
        }

        private void LogException(Exception e, string details)
        {
            var errorMessage = $"{details}: {e}";
            _log.LogError(errorMessage);
            Console.Error.WriteLine(errorMessage);
        }

        private void LogDataError(ArraySegment<byte> data)
        {
            var messageBuilder = new System.Text.StringBuilder();
            if (data.Array == null)
            {
                messageBuilder.AppendLine("Message with no data array");
            }
            else
            {
                messageBuilder.Append($"Message with data ({data.Count} bytes): ");

                //HINT: Slow but readable
                for (var i = 0; i < data.Count && i < _settings.MessagePreviewSize; i++)
                {
                    messageBuilder.AppendFormat("{0:X2}", data.Array[data.Offset + i]);
                }
            }

            var logMessage = messageBuilder.ToString();
            _log.LogError(logMessage);
            Console.Error.WriteLine(logMessage);
        }
    }
}