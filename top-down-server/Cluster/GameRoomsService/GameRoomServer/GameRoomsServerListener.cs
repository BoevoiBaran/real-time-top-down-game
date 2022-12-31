using System.Threading.Tasks;
using GameRoomServer.Shared;

namespace GameRoomsService.GameRoomServer
{ 
    public interface IGameRoomsServerListener : IProgressIdFetcher { }

    public class GameRoomsServerListener : IGameRoomsServerListener
    {
        public Task<string> FetchAsync(string token)
        {
            return Task.FromResult(token);
        }
    }
}