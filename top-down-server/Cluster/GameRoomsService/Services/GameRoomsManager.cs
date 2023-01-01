using System.Threading;
using System.Threading.Tasks;
using GameRoomsService.GameRoomServer;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameRoomsService.Services
{
    public class GameRoomsManager : BackgroundService
    {
        private readonly ILogger<GameRoomsManager> _logger;
        private readonly RoomsHolder _roomsHolder;
        private readonly GameRoomsServer _gameRoomsServer;
        
        public GameRoomsManager(
            GameRoomsServer gameRoomsServer,
            RoomsHolder roomsHolder, 
            ILogger<GameRoomsManager> logger)
        {
            _roomsHolder = roomsHolder;
            _logger = logger;
            _gameRoomsServer = gameRoomsServer;
        }
        
        private async Task InitializeServer()
        {
            await _gameRoomsServer.Initialize();
            
            _logger.LogInformation("[GameRoomsManager] GameRoomsServer was initialized");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await InitializeServer();
            
            var dialogTask = _gameRoomsServer.Run(stoppingToken);
            
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
                _logger.LogInformation("Do work");
            }
            
            await dialogTask.WaitAsync(stoppingToken);
        }
    }
}