using GameRoomServer.Shared;
using ProtoBuf.Meta;
using Telepathy;

namespace GameRoomsService.GameRoomServer
{
    internal class GameRoomsNetworkServer : ServerEngine
    {
        public GameRoomsNetworkServer(GameRoomsServerSettings settings, IProgressIdFetcher progressIdFetcher, IServerMessageProcessor messageProcessor, ILog log)
            : base(settings, progressIdFetcher, messageProcessor, log) { }

        protected override uint NetProtocolVersion => 1;

        protected override void InitProtobufSerialization()
        {
            var typeModel = RuntimeTypeModel.Default;
            ProtoBufInitialize.Initialize(typeModel);
        }
    }
}