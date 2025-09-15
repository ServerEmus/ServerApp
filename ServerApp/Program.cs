using ModdableWebServer.Helper;
using NetCoreServer;
using ServerShared.CommonModels;
using ServerShared.Controllers;
using Shared;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace ServerApp;
internal class Program
{
    public static Settings ServerAppSettings = JsonController.Read<Settings>("ServerAppSettings.json");
    static void Main(string[] args)
    {
        MainLogger.CreateNew();
        ArgProcess.Process(args);
        X509Certificate2Collection Collection = [];
        foreach (var cert in Directory.GetFiles("Cert"))
        {
            if (cert.EndsWith("key"))
                continue;
            var certDetail = ServerAppSettings.CertDetails.FirstOrDefault(x => cert.Contains(x.Name));
            string password = string.Empty;
            if (certDetail != null)
                password = certDetail.Password;
            if (cert.EndsWith("pfx"))
            {
                Collection.Add(CertHelper.GetCert(cert, password));
                continue;
            }
            var name = Path.GetFileNameWithoutExtension(cert);
            var keyName = Path.Combine("Cert", $"{name}.key");
            Collection.Add(CertHelper.GetCertPem(cert, keyName));
        }
        SslContext context = new(SslProtocols.Tls12, Collection, new(CertHelper.NoCertificateValidator));

        List<ServerModel> servers = [];

        foreach (var server in ServerAppSettings.Servers)
        {
            servers.Add(new()
            { 
                Name  = server.Name,
                Port = server.Port,
                Context = server.UseCerts ? context : null,
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