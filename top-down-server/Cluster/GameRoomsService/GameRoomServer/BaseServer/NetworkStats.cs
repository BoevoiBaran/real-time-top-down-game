using System;
using System.Collections.Generic;

namespace GameRoomsService.GameRoomServer
{
    public struct NetworkStats
    {
        public IReadOnlyDictionary<int, DateTime> Clients;

        public TimeSpan Runtime;
        public TimeSpan RuntimeDelta;
        public int MessagesReceived;
        public int MessagesReceivedDelta;
        public int MessagesSent;
        public int MessagesSentDelta;
        public int MessagesInQueue;
        public long BytesReceived;
        public long BytesReceivedDelta;
        public long BytesSent;
        public long BytesSentDelta;
    }
}