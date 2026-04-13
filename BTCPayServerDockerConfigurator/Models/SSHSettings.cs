using Renci.SshNet;
using SshConnectionInfo = Renci.SshNet.ConnectionInfo;

namespace BTCPayServerDockerConfigurator.Models;

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
    public string PrivateKeyContent { get; set; }

    public SshConnectionInfo CreateConnectionInfo()
    {
        if (!string.IsNullOrEmpty(PrivateKeyContent))
        {
            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(PrivateKeyContent));
            var keyFile = string.IsNullOrEmpty(KeyFilePassword)
                ? new PrivateKeyFile(stream)
                : new PrivateKeyFile(stream, KeyFilePassword);
            return new SshConnectionInfo(Server, Port, Username,
                new PrivateKeyAuthenticationMethod(Username, keyFile));
        }

        if (!string.IsNullOrEmpty(KeyFile))
        {
            return new SshConnectionInfo(Server, Port, Username,
                new PrivateKeyAuthenticationMethod(Username,
                    new PrivateKeyFile(KeyFile, KeyFilePassword)));
        }

        return new SshConnectionInfo(Server, Port, Username,
            new PasswordAuthenticationMethod(Username, Password));
    }
}
