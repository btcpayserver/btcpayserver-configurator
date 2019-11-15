namespace BTCPayServerDockerConfigurator.Models
{
    public class Options
    {
        public string SSHConnection { get; set; } = null;
        public string SSHPassword { get; set; } = "";
        public string SSHKeyFile { get; set; }= "";
        public string SSHAuthorizedKeys { get; set; }= "";
        public string SSHKeyFilePassword { get; set; }= "";
        
        public SSHSettings ParseSSHConfiguration()
        {
            var settings = new SSHSettings()
            {
                Password = SSHPassword,
                KeyFile = SSHKeyFile,
                AuthorizedKeysFile = SSHAuthorizedKeys,
                KeyFilePassword = SSHKeyFilePassword,
                Server = SSHConnection
            };
            if (settings.Server != null)
            {
                var parts = settings.Server.Split(':');
                if (parts.Length == 2 && int.TryParse(parts[1], out int port))
                {
                    settings.Port = port;
                    settings.Server = parts[0];
                }
                else
                {
                    settings.Port = 22;
                }

                parts = settings.Server.Split('@');
                if (parts.Length == 2)
                {
                    settings.Username = parts[0];
                    settings.Server = parts[1];
                }
                else
                {
                    settings.Username = "root";
                }
            }
            return settings;
        }
    }
}