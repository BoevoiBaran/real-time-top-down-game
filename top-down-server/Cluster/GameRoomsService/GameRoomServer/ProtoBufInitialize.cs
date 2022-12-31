using GameRoomServer.Shared;
using ProtoBuf.Meta;
using Shared.Network;

namespace GameRoomsService.GameRoomServer
{
    public static class ProtoBufInitialize
    {
        public static void Initialize(RuntimeTypeModel serializerModel)
        {
            serializerModel[typeof(BaseMessage)].AddSubType(BaseMessage.PROTO_VACANT_INDEX + 1, typeof(GameRoomNetMessage));
        }
    }
}