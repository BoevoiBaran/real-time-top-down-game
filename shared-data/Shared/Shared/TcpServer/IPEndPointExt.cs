using System;
using System.Net;

namespace GameRoomServer.Shared
{
    // ReSharper disable once InconsistentNaming
    public static class IPEndPointExt
    {
        public static IPEndPoint FromString(string addressString)
        {
            if (string.IsNullOrEmpty(addressString))
                throw new ArgumentException("Empty address");

            var addressParts = addressString.Split(':');
            if (addressParts.Length < 2)
                throw new FormatException("Invalid address");

            IPAddress ipAddress;
            if (addressParts.Length > 2)
            {
                if (!IPAddress.TryParse(string.Join(":", addressParts, 0, addressParts.Length - 1), out ipAddress))
                {
                    throw new FormatException("Invalid IP6 address");
                }
            }
            else
            {
                if (!IPAddress.TryParse(addressParts[0], out ipAddress))
                {
                    throw new FormatException("Invalid IP4 address");
                }
            }

            if (!ushort.TryParse(addressParts[addressParts.Length - 1], out var port))
            {
                throw new FormatException("Invalid port");
            }

            return new IPEndPoint(ipAddress, port);
        }

        public static void FromStringOption(string addressString, string optionName, Action<IPEndPoint> applyAction)
        {
            if (string.IsNullOrEmpty(addressString))
                return;

            try
            {
                var address = FromString(addressString);
                applyAction?.Invoke(address);
            }
            catch (FormatException)
            {
                throw new FormatException($"{optionName} ({addressString}) must be a valid IP4/IP6 address.");
            }
        }
    }
}