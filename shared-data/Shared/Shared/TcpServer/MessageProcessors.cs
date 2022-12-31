using System.Threading.Tasks;

namespace GameRoomServer.Shared
{
    public interface IProgressIdFetcher
    {
        /// <summary>
        /// Get progress id from auth server
        /// </summary>
        /// <param name="token">Authentication token</param>
        /// <returns></returns>
        Task<string> FetchAsync(string token);
    }
    
    public interface IServerMessageProcessor
    {
        Task ConvertMessage(BaseMessage message, int connectionId);
    }
    
    public interface IClientMessageProcessor
    {
        void ConvertMessage(BaseMessage message);
    }
}