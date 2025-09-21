using ModdableWebServer.Helper;
using Serilog;
using ServerShared.CommonModels;
using ServerShared.Controllers;
using Shared;
using System.Security.Cryptography.X509Certificates;

namespace ServerApp;
internal class Program
{
    public static Settings ServerAppSettings = JsonController.Read<Settings>("ServerAppSettings.json");
    static void Main(string[] args)
    {
        MainLogger.CreateNew();
        ArgProcess.Process(args);
        foreach (var cert in Directory.GetFiles("Cert", "*.*", SearchOption.AllDirectories))
        {
            if (cert.EndsWith(".key"))
                continue;
            var certDetail = ServerAppSettings.CertDetails.FirstOrDefault(x => cert.Contains(x.Name));
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

        List<ServerModel> servers = [];

        foreach (var server in ServerAppSettings.Servers)
        {
            Log.Information($"New Server {server.Name} registered!");
            servers.Add(new()
            { 
                Name  = server.Name,
                Port = server.Port,
                IsSecure = server.UseCerts,
                IsUdp = server.UseUDP,
            });
        }

        ServerController.Start(servers);
        PluginController.LoadPlugins();
        List<string> quitList =
        [
            "q",
            "quit",
            "exit",
        ];
        string endCheck = string.Empty;
        while (!quitList.Contains(endCheck))
        {
            endCheck = Console.ReadLine()!;
            if (endCheck.StartsWith('!'))
            {
                CommandController.Run(endCheck[1..]);
            }
        }
        PluginController.StopPlugins();
        ServerController.Stop();
        MainLogger.Close();
    }

}