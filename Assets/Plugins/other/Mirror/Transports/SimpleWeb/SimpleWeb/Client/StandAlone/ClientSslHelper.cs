﻿using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace Mirror.SimpleWeb
{
    internal class ClientSslHelper
    {
        internal bool TryCreateStream(Connection conn, Uri uri)
        {
            var stream = conn.client.GetStream();
            if (uri.Scheme != "wss")
            {
                conn.stream = stream;
                return true;
            }

            try
            {
                conn.stream = CreateStream(stream, uri);
                return true;
            }
            catch (Exception e)
            {
                Log.Error($"[SimpleWebTransport] Create SSLStream Failed: {e}", false);
                return false;
            }
        }

        private Stream CreateStream(NetworkStream stream, Uri uri)
        {
            var sslStream = new SslStream(stream, true, ValidateServerCertificate);
            sslStream.AuthenticateAsClient(uri.Host);
            return sslStream;
        }

        private static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            // Do not allow this client to communicate with unauthenticated servers.

            // only accept if no errors
            return sslPolicyErrors == SslPolicyErrors.None;
        }
    }
}