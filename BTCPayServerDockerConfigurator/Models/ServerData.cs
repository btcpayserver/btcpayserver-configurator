using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Renci.SshNet;

namespace BTCPayServerDockerConfigurator.Models
{
    public class ServerData
    {
        public const string FetchMemoryCommand = "grep MemTotal: /proc/meminfo | grep -E -o '[0-9]+'";
        public const string FetchDisksCommand = "lsblk --json";
        public const string CheckDockerInstalledCommand = "which docker";
        public const string GetMountPointForLibFolderCommand  = "findmnt -n -o SOURCE --target /var/lib/";
        public const string GetMountPointForVolumeFolderCommand  = "findmnt -n -o SOURCE --target /var/lib/docker/volumes/";
        public const string DiskFreeForLibFolderCommand  = "df /var/lib/";
        public const string DiskFreeForVolumeFolderCommand  = "df /var/lib/docker/volumes/";
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
            var cmd = await
                ssh.RunBash(FetchMemoryCommand);
            if (cmd.ExitStatus == 0 && int.TryParse(cmd.Output, out var memKb ))
            {
                result.MemoryBytes = Convert.ToInt64(memKb * 1000);
            }
            
            cmd = await
                ssh.RunBash(FetchDisksCommand);
            if (cmd.ExitStatus == 0)
            {
                result.StorageList = JsonSerializer.Deserialize<BlockDevicesList>(cmd.Output);
            }
            
            cmd = await
                ssh.RunBash(CheckDockerInstalledCommand);
            result.DockerInstalled = cmd.ExitStatus == 0;
            
            cmd = await
                ssh.RunBash(GetMountPointForLibFolderCommand );
            result.LibFolderMount = cmd.Output;

            if (result.DockerInstalled is true)
            {
                cmd = await
                    ssh.RunBash(GetMountPointForVolumeFolderCommand );
                result.VolumeFolderMount = cmd.Output;
            }            
            cmd = await
                ssh.RunBash(DiskFreeForLibFolderCommand );

            result.DiskFreeForLib = DiskFreeResult.Parse(cmd.Output);

            if (result.DockerInstalled is true)
            {
                cmd = await
                    ssh.RunBash(DiskFreeForVolumeFolderCommand );
                result.DiskFreeForVolume = DiskFreeResult.Parse(cmd.Output);
            }

            result.Loaded = true;
            return result;
        }
        
        public static String BytesToString(long byteCount)
        {
            string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" }; //Longs run out around EB
            if (byteCount == 0)
                return "0" + suf[0];
            long bytes = Math.Abs(byteCount);
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return (Math.Sign(byteCount) * num).ToString() + suf[place];
        }



        public class DiskFreeResult
        {
            public string  FileSystem { get; set; }
            public long  Total { get; set; }
            public long  Used { get; set; }
            public int  UsedPercentage { get; set; }
            public long  Available { get; set; }
            public string Mountpoint { get; set; }

            public static DiskFreeResult Parse(string res)
            {
                //trim the column header
                var line = res.Replace(Environment.NewLine, "").Split("\n", StringSplitOptions.RemoveEmptyEntries)
                    .Last();
                var split = line.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                return new DiskFreeResult()
                {
                    FileSystem = split[0],
                    Total = long.Parse(split[1]) * 1000,
                    Used = long.Parse(split[2]) * 1000,
                    Available = long.Parse(split[3]) * 1000,
                    UsedPercentage = int.Parse(split[4].TrimEnd('%')),
                    Mountpoint =  split[5]
                };
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
            public string Rm { get; set; }

            [JsonPropertyName("size")]
            public string Size { get; set; }

            [JsonPropertyName("ro")]
            public string Ro { get; set; }

            [JsonPropertyName("type")]
            public string Type { get; set; }

            [JsonPropertyName("mountpoint")]
            public string Mountpoint { get; set; }

            [JsonPropertyName("children")]
            public IList<BlockDevice> Children { get; set; }
        }
    }
}