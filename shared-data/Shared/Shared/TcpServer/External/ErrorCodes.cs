namespace GameRoomServer.Shared
{
    public enum ErrorCodes
    {
        Unknown = 0,
        // Codes used by lib
        VersionMismatch = ErrorCodeValues.VERSION_MISMATCH,
        AuthAnotherDevice = ErrorCodeValues.AUTH_ANOTHER_DEVICE,
        // define project-specific error codes below
    }
}