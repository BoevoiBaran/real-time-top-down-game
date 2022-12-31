using System.Collections.Generic;

namespace GameRoomsService.Services
{
    public class RoomsHolder
    {
        public IEnumerable<int> ActiveConnections { get; set; }

        public void AddClient(int connectionId, object convertedClientId, string roomId)
        {
            throw new System.NotImplementedException();
        }

        public bool TryGetClientId(int connectionId, out object o)
        {
            throw new System.NotImplementedException();
        }

        public void RemoveClient(int connectionId)
        {
            throw new System.NotImplementedException();
        }
    }
}