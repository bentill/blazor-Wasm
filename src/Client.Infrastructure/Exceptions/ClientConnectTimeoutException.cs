using System;

namespace Client.Infrastructure.Exceptions
{
    public class ClientConnectTimeoutException : Exception
    {
        public ClientConnectTimeoutException(string serverName, int port, TimeSpan timeout)
            : base($"Could not connect to server {serverName}:{port} in {timeout.TotalSeconds}s")
        {
        }
    }
}
