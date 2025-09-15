namespace ServerApp;

internal class Settings
{
    public class Server
    {
        public string Name { get; set; } = string.Empty;
        public int Port { get; set; }
        public bool UseUDP { get; set; } = false;
        public bool UseCerts { get; set; }
    }

    public class CertDetail
    {
        public string Name { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;
    }

    public List<Server> Servers { get; set; } = [];

    public List<CertDetail> CertDetails { get; set; } = [];
}
