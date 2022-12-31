namespace GameRoomsService.GameRoomServer
{
    public class GameRoomsServerSettings
    {
        public int Port { get; set; }

        public int TickPeriod { get; set; }
        public int PingPeriod { get; set; }

        public int MaxMessageSize { get; set; }
        public int MaxMessagesPerTick { get; set; }

        public int StatsHangingClientMinutes { get; set; }
        public int MessagePreviewSize { get; set; }

        public GameRoomsServerSettings()
        {
            Port = 5002;
            TickPeriod = 10;

            MaxMessageSize = 4096;
            MaxMessagesPerTick = 256;

            StatsHangingClientMinutes = 60;
            MessagePreviewSize = 64;
        }
    }
}