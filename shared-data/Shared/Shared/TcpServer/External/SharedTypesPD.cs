using System;
using ProtoBuf;
using ProtoBuf.Meta;
 
namespace GameRoomServer.Shared
{
    ///
    /// This file is used to defile project-dependent Message types. 
    /// Derive your messages from Message, QueryMessage or AnswerMessage
    ///
    [ProtoContract]
    public abstract class QueryMessage : Message
    {
        public override Type Type => GetType();
    }

    [ProtoContract]
    public abstract class AnswerMessage : Message
    {
        public override Type Type => GetType();
    }
    
    [ProtoInclude(101, typeof(AnswerMessage))]
    [ProtoInclude(102, typeof(QueryMessage))]
    [ProtoContract]
    public abstract class Message : BaseMessage
    {
        public override Type Type => GetType();
    }
    
    public static class ProtoBufInit
    {
        private static bool _wasInitialized = false;
        public static void Init()
        {
            if (_wasInitialized)
            {
                return;
            }

            RuntimeTypeModel.Default[typeof(BaseMessage)]
                .AddSubType(BaseMessage.PROTO_VACANT_INDEX, typeof(Message));
            _wasInitialized = true;
        }
    }
}