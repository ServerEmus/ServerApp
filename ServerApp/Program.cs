using Serilog;
using ServerShared.CommonModels;
using ServerShared.Controllers;
using Shared;

namespace ServerApp;
internal class Program
{
    public static Settings ServerAppSettings = JsonController.Read<Settings>("ServerAppSettings.json");
    static void Main(string[] args)
    {
        MainLogger.CreateNew();
        ArgProcess.Process(args);
        CertLoader.Load();

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