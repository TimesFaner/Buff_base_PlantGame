using System.IO;
using System.Security.Authentication;
using UnityEngine;

namespace Mirror.SimpleWeb
{
    public class SslConfigLoader
    {
        public static SslConfig Load(bool sslEnabled, string sslCertJson, SslProtocols sslProtocols)
        {
            // don't need to load anything if ssl is not enabled
            if (!sslEnabled)
                return default;

            var certJsonPath = sslCertJson;

            var cert = LoadCertJson(certJsonPath);

            return new SslConfig(
                sslEnabled,
                sslProtocols: sslProtocols,
                certPath: cert.path,
                certPassword: cert.password
            );
        }

        internal static Cert LoadCertJson(string certJsonPath)
        {
            var json = File.ReadAllText(certJsonPath);
            var cert = JsonUtility.FromJson<Cert>(json);

            if (string.IsNullOrWhiteSpace(cert.path))
                throw new InvalidDataException("Cert Json didn't not contain \"path\"");

            // don't use IsNullOrWhiteSpace here because whitespace could be a valid password for a cert
            // password can also be empty
            if (string.IsNullOrEmpty(cert.password))
                cert.password = string.Empty;

            return cert;
        }

        internal struct Cert
        {
            public string path;
            public string password;
        }
    }
}