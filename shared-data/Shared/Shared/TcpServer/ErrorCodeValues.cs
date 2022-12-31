namespace GameRoomServer.Shared
{
    public static class ErrorCodeValues
    {
        public const int VERSION_MISMATCH = int.MaxValue - 1;
        public const int AUTH_ANOTHER_DEVICE = int.MaxValue - 2;
        public const int AUTH_FAILED = int.MaxValue - 3;
    }
}