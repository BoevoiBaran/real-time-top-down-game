using System;
using ProtoBuf;

namespace GameRoomServer.Shared
{
      
    // 50 is SharedType.PROTO_VACANT_INDEX
    [ProtoContract]
    [ProtoInclude(50 - 1, typeof(ErrorMessage))]
    [ProtoInclude(50 - 2, typeof(ConnectMessage))]
    [ProtoInclude(50 - 3, typeof(ConnectQueryMessage))]
    [ProtoInclude(50 - 4, typeof(NetworkPingMessage))]
    public abstract class BaseMessage : object
    {
        public static int PROTO_VACANT_INDEX = 50;
        [ProtoMember(1)] public uint Id;
        [ProtoMember(2)] public uint ResponseTo;
        public abstract Type Type { get; }
    }
    
    [ProtoContract]
    public class ConnectMessage : BaseMessage
    {
        [ProtoMember(3)] public bool IsOK;
        public override         Type Type => GetType();
    }

    [ProtoContract]
    public class ErrorMessage : BaseMessage
    {
        [ProtoMember(3)] public int Code;
        [ProtoMember(4)] public string Message;
        public override         Type   Type => GetType();
    }
    
    [ProtoContract]
    public class ConnectQueryMessage : BaseMessage
    {
        [ProtoMember(3)] public uint   Version;
        [ProtoMember(4)] public string Token;
        public override         Type   Type => GetType();
    }

    [ProtoContract]
    public class NetworkPingMessage : BaseMessage
    {
        [ProtoMember(3)] public long Tick;
        public override Type Type => GetType();
    }
}
