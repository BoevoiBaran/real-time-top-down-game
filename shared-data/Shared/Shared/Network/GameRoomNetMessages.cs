using System;
using GameRoomServer.Shared;
using ProtoBuf;

namespace Shared.Network
{
    [ProtoInclude(100, typeof(GameRoomStateMessage))]
    [ProtoContract]
    public abstract class GameRoomNetMessage : BaseMessage
    {
        public override Type Type => GetType();
    }

    [ProtoContract]
    public class GameRoomStateMessage : GameRoomNetMessage
    {
        
    }
}