using System;
using System.IO;
using ProtoBuf.Meta;

namespace GameRoomServer.Shared
{
    public static class GameRoomServerExtensions
    {
        public static ArraySegment<byte> SerializeWithDefaultTypeModel<T>(this T message) where T : BaseMessage
        {
            return message.Serialize(RuntimeTypeModel.Default);
        }

        public static ArraySegment<byte> Serialize<T>(this T message, TypeModel typeModel) where T : BaseMessage
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (typeModel == null)
            {
                throw new ArgumentNullException(nameof(typeModel));
            }

            byte[] data;
            using (var stream = new MemoryStream())
            {
                typeModel.Serialize(stream, message);
                data = stream.ToArray();
            }

            var result = new ArraySegment<byte>(data);
            return result;
        }

        public static BaseMessage Deserialize(this ArraySegment<byte> data, TypeModel typeModel)
        {
            if (data.Array == null || data.Count == 0)
            {
                throw new NotSupportedException("Empty data in ArraySegment");
            }

            if (typeModel == null)
            {
                throw new ArgumentNullException(nameof(typeModel));
            }

            BaseMessage result;

            using (var stream = new MemoryStream(data.Array, data.Offset, data.Count))
            {
                result = typeModel.Deserialize(stream, null, typeof(BaseMessage)) as BaseMessage;
            }

            return result;
        }
    }
}