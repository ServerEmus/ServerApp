using ModdableWebServer.Helper;
using Serilog;
using ServerShared.Controllers;
using System.Security.Cryptography.X509Certificates;

namespace ServerApp;

internal static class CertLoader
{
    public static void Load()
    {
        if (!Directory.Exists("Cert"))
            Directory.CreateDirectory("Cert");
        foreach (var cert in Directory.GetFiles("Cert", "*.*", SearchOption.AllDirectories))
        {
            if (cert.EndsWith(".key"))
                continue;
            var certDetail = Program.ServerAppSettings.CertDetails.FirstOrDefault(x => cert.Contains(x.Name));
            if (certDetail == null)
                continue;
            string password = string.Empty;
            if (!string.IsNullOrEmpty(certDetail.Password))
                password = certDetail.Password;
            if (cert.EndsWith(".pfx"))
            {
                Log.Information("Adding cert: {cert}", cert);
                X509Certificate2 cert2 = (CertHelper.GetCert(cert, password) as X509Certificate2)!;
                if (certDetail.IsMainCert)
                {
                    ServerController.MainCertificate = cert2;
                    continue;
                }
                ServerController.Certificates.Add(cert2);
                continue;
            }
            var name = Path.GetFileNameWithoutExtension(cert);
            var keyName = Path.Combine("Cert", $"{name}.key");
            var cert1 = Path.Combine(Directory.GetCurrentDirectory(), cert);
            keyName = Path.Combine(Directory.GetCurrentDirectory(), keyName);
            Log.Information("Adding cert: {cert}, {key}", cert1, keyName);
            var certificate = X509Certificate2.CreateFromPemFile(cert1, keyName);
            if (string.IsNullOrEmpty(password))
                password = "test";
            var pfx = certificate.Export(X509ContentType.Pfx, password);
            certificate = X509CertificateLoader.LoadPkcs12(pfx, password);
            if (certDetail.IsMainCert)
            {
                ServerController.MainCertificate = certificate;
                continue;
            }
            ServerController.Certificates.Add(certificate);
        }
    }
}
