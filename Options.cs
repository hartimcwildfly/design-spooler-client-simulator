using System.Net;
using DesignSpoolerClientSimulator.Resources;

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
                    Console.Error.WriteLine(string.Format(Messages.UnknownArgument, args[i]));
                    PrintUsage();
                    return null;
            }
        }

        return options;
    }

    private static void PrintUsage()
    {
        Console.WriteLine(Messages.Usage);
    }
}
