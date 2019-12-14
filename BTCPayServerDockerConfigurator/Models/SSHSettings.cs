using Renci.SshNet;

namespace BTCPayServerDockerConfigurator.Models
{
    public class SSHSettings
    {
        public string Server { get; set; }
        public int Port { get; set; } = 22;
        public string KeyFile { get; set; }
        public string KeyFilePassword { get; set; }
        public string AuthorizedKeysFile { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string RootPassword { get; set; }
        

        public ConnectionInfo CreateConnectionInfo()
        {
            if (!string.IsNullOrEmpty(KeyFile))
            {
                return new ConnectionInfo(Server, Port, Username, new[] { new PrivateKeyAuthenticationMethod(Username, new PrivateKeyFile(KeyFile, KeyFilePassword)) });
            }
            else
            {
                return new ConnectionInfo(Server, Port, Username, new[] { new PasswordAuthenticationMethod(Username, Password) });
            }
        }
    }
}