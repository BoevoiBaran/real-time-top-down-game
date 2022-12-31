using System;
using System.Threading;
using System.Threading.Tasks;
using GameRoomServer.Shared;

namespace GameRoomsService.GameRoomServer
{
    public interface INetworkServer
    {
        event Action<BaseMessage, int> OnGetMessage;
        event Action<int> OnClientDisconnected;

        Task Run(CancellationToken cancellationToken);
        bool Send(int connectionId, ArraySegment<byte> data);
        int GetSendQueueCount(int connectionId);
        bool DisconnectClient(int connectionId);
        bool Disconnect(int connectionId);
        void ForgetClientAsNonAuth(int connectionId);
        bool IsConnectionAuthorized(int connectionId);

        NetworkStats GetStats();
        void ClearStats();
    }
}