using System.Text.Json;
using System.Text.Json.Serialization;
using Renci.SshNet;

namespace BTCPayServerDockerConfigurator.Models;

public class ServerData
{
    public const string FetchMemoryCommand = "grep MemTotal: /proc/meminfo | grep -E -o '[0-9]+'";
    public const string FetchDisksCommand = "lsblk --json";
    public const string CheckDockerInstalledCommand = "which docker";
    public const string GetMountPointForLibFolderCommand = "findmnt -n -o SOURCE --target /var/lib/";
    public const string GetMountPointForVolumeFolderCommand = "findmnt -n -o SOURCE --target /var/lib/docker/volumes/";
    public const string DiskFreeForLibFolderCommand = "df /var/lib/";
    public const string DiskFreeForVolumeFolderCommand = "df /var/lib/docker/volumes/";
    public long? MemoryBytes { get; set; }
    public BlockDevicesList StorageList { get; set; }
    public bool? DockerInstalled { get; set; }
    public string LibFolderMount { get; set; }
    public string VolumeFolderMount { get; set; }
    public DiskFreeResult DiskFreeForVolume { get; set; }
    public DiskFreeResult DiskFreeForLib { get; set; }
    public bool Loaded { get; set; }

    public static async Task<ServerData> Load(SshClient ssh)
    {
        var result = new ServerData();
        var cmd = await ssh.RunBash(FetchMemoryCommand);
        if (cmd.ExitStatus == 0 && long.TryParse(cmd.Output?.Trim(), out var memKb))
        {
            result.MemoryBytes = memKb * 1000;
        }

        cmd = await ssh.RunBash(FetchDisksCommand);
        if (cmd.ExitStatus == 0)
        {
            try
            {
                result.StorageList = JsonSerializer.Deserialize<BlockDevicesList>(cmd.Output);
            }
            catch (JsonException)
            {
            }
        }

        cmd = await ssh.RunBash(CheckDockerInstalledCommand);
        result.DockerInstalled = cmd.ExitStatus == 0;

        cmd = await ssh.RunBash(GetMountPointForLibFolderCommand);
        result.LibFolderMount = cmd.Output;

        if (result.DockerInstalled is true)
        {
            cmd = await ssh.RunBash(GetMountPointForVolumeFolderCommand);
            result.VolumeFolderMount = cmd.Output;
        }

        cmd = await ssh.RunBash(DiskFreeForLibFolderCommand);
        result.DiskFreeForLib = DiskFreeResult.TryParse(cmd.Output);

        if (result.DockerInstalled is true)
        {
            cmd = await ssh.RunBash(DiskFreeForVolumeFolderCommand);
            result.DiskFreeForVolume = DiskFreeResult.TryParse(cmd.Output);
        }

        result.Loaded = true;
        return result;
    }

    public static string BytesToString(long byteCount)
    {
        string[] suf = ["B", "KB", "MB", "GB", "TB", "PB", "EB"];
        if (byteCount == 0)
            return "0" + suf[0];
        long bytes = Math.Abs(byteCount);
        int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
        double num = Math.Round(bytes / Math.Pow(1024, place), 1);
        return (Math.Sign(byteCount) * num) + suf[place];
    }

    public class DiskFreeResult
    {
        public string FileSystem { get; set; }
        public long Total { get; set; }
        public long Used { get; set; }
        public int UsedPercentage { get; set; }
        public long Available { get; set; }
        public string Mountpoint { get; set; }

        public static DiskFreeResult TryParse(string res)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(res))
                    return null;

                var line = res.Replace(Environment.NewLine, "\n")
                    .Split("\n", StringSplitOptions.RemoveEmptyEntries)
                    .Last();
                var split = line.Split(" ", StringSplitOptions.RemoveEmptyEntries);

                if (split.Length < 6)
                    return null;

                if (!long.TryParse(split[1], out var total) ||
                    !long.TryParse(split[2], out var used) ||
                    !long.TryParse(split[3], out var available) ||
                    !int.TryParse(split[4].TrimEnd('%'), out var usedPercentage))
                    return null;

                return new DiskFreeResult
                {
                    FileSystem = split[0],
                    Total = total * 1000,
                    Used = used * 1000,
                    Available = available * 1000,
                    UsedPercentage = usedPercentage,
                    Mountpoint = split[5]
                };
            }
            catch
            {
                return null;
            }
        }
    }

    public class BlockDevicesList
    {
        [JsonPropertyName("blockdevices")]
        public IList<BlockDevice> Blockdevices { get; set; }
    }

    public class BlockDevice
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("maj:min")]
        public string MajMin { get; set; }

        [JsonPropertyName("rm")]
        public JsonElement? Rm { get; set; }

        [JsonPropertyName("size")]
        public JsonElement? Size { get; set; }

        [JsonPropertyName("ro")]
        public JsonElement? Ro { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("mountpoint")]
        public string Mountpoint { get; set; }

        [JsonPropertyName("children")]
        public IList<BlockDevice> Children { get; set; }
    }
}
