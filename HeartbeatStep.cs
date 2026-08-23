using System.Net;
using System.Net.Sockets;

namespace DesignSpoolerClientSimulator;

public static class HeartbeatStep
{
    /// <summary>
    /// After the handshake, the real client keeps sending a unicast UDP
    /// packet to the server's own IP every ~2 seconds for as long as the
    /// connection is active (pcap #20 onward). The payload of those packets
    /// is session-encrypted and can't be regenerated, so this replays the
    /// same discovery bytes as a structural "is the server's UDP listener
    /// still alive and answering" probe rather than a protocol-accurate
    /// heartbeat.
    /// </summary>
    public static async Task RunAsync(IPAddress serverIp, Options options, CancellationToken ct)
    {
        using var udp = new UdpClient();
        udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        udp.Client.Bind(new IPEndPoint(IPAddress.Any, options.LocalUdpPort));

        var request = CapturedBytes.DiscoveryRequest;
        var target = new IPEndPoint(serverIp, options.MulticastPort);
        var count = 0;

        try
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(options.HeartbeatInterval, ct);
                count++;
                await udp.SendAsync(request, target, ct);

                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeoutCts.CancelAfter(TimeSpan.FromMilliseconds(500));
                try
                {
                    var result = await udp.ReceiveAsync(timeoutCts.Token);
                    Console.WriteLine($"[heartbeat #{count}] reply: {result.Buffer.Length} bytes from {result.RemoteEndPoint}");
                }
                catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                {
                    Console.WriteLine($"[heartbeat #{count}] no reply (server may only reply once per session, or heartbeat isn't answered - check against your own capture)");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Ctrl+C - fall through
        }

        Console.WriteLine($"[heartbeat] stopped after {count} probe(s)");
    }
}
