using System.Net;

namespace DesignSpoolerClientSimulator;

public sealed class Options
{
    public IPAddress? ServerIp { get; private set; }
    public IPAddress MulticastAddress { get; private set; } = IPAddress.Parse("234.5.6.7");
    public int MulticastPort { get; private set; } = 4564;
    public int LocalUdpPort { get; private set; } = 3001;
    public int TcpPort { get; private set; } = 9050;
    public TimeSpan DiscoveryTimeout { get; private set; } = TimeSpan.FromSeconds(5);
    public TimeSpan DiscoverySettleTime { get; private set; } = TimeSpan.FromMilliseconds(750);
    public TimeSpan TcpTimeout { get; private set; } = TimeSpan.FromSeconds(5);
    public TimeSpan HeartbeatInterval { get; private set; } = TimeSpan.FromSeconds(2);
    public bool NoHeartbeat { get; private set; }

    public static Options? Parse(string[] args)
    {
        var options = new Options();

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--server":
                    options.ServerIp = IPAddress.Parse(args[++i]);
                    break;
                case "--multicast-ip":
                    options.MulticastAddress = IPAddress.Parse(args[++i]);
                    break;
                case "--multicast-port":
                    options.MulticastPort = int.Parse(args[++i]);
                    break;
                case "--local-udp-port":
                    options.LocalUdpPort = int.Parse(args[++i]);
                    break;
                case "--tcp-port":
                    options.TcpPort = int.Parse(args[++i]);
                    break;
                case "--discovery-timeout":
                    options.DiscoveryTimeout = TimeSpan.FromSeconds(double.Parse(args[++i]));
                    break;
                case "--discovery-settle":
                    options.DiscoverySettleTime = TimeSpan.FromSeconds(double.Parse(args[++i]));
                    break;
                case "--heartbeat-interval":
                    options.HeartbeatInterval = TimeSpan.FromSeconds(double.Parse(args[++i]));
                    break;
                case "--no-heartbeat":
                    options.NoHeartbeat = true;
                    break;
                case "-h":
                case "--help":
                    PrintUsage();
                    return null;
                default:
                    Console.Error.WriteLine($"Unknown argument: {args[i]}");
                    PrintUsage();
                    return null;
            }
        }

        return options;
    }

    private static void PrintUsage()
    {
        Console.WriteLine("""
            Usage: DesignSpoolerClientSimulator [options]

              --server <ip>              Skip UDP discovery, connect straight to this server IP.
              --multicast-ip <ip>        Discovery multicast group (default 234.5.6.7).
              --multicast-port <port>    Discovery/heartbeat UDP port (default 4564).
              --local-udp-port <port>    Local UDP port to bind for discovery (default 3001, as in the capture).
              --tcp-port <port>          Server TCP control port (default 9050).
              --discovery-timeout <sec>  How long to wait for a discovery reply (default 5).
              --discovery-settle <sec>   After the first reply, how long to keep listening for a
                                         second/rogue server before giving up (default 0.75).
              --heartbeat-interval <sec> Delay between heartbeat UDP packets (default 2, as in the capture).
              --no-heartbeat             Skip the continuous post-handshake heartbeat loop.
            """);
    }
}
