using System.Net;
using System.Net.Sockets;

namespace DesignSpoolerClientSimulator;

public static class DiscoveryStep
{
    /// <summary>
    /// Replays the captured multicast discovery request and waits for unicast
    /// replies, mirroring packets #4 (request) and #6 (response) in the
    /// capture. Exactly one server is expected to answer - the embroidery
    /// machine has no way to pick between several, so more than one reply is
    /// treated as a network misconfiguration rather than a warning.
    ///
    /// To detect a second responder, this keeps listening for a short "settle"
    /// window after the first reply instead of returning immediately, so a
    /// slightly slower second server still gets caught. Returns the single
    /// responding server's IP address, or null if zero or more than one
    /// server replied.
    /// </summary>
    public static async Task<IPAddress?> RunAsync(Options options, CancellationToken ct)
    {
        using var udp = new UdpClient();
        udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        udp.Client.Bind(new IPEndPoint(IPAddress.Any, options.LocalUdpPort));
        udp.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 1);

        var request = CapturedBytes.DiscoveryRequest;
        var target = new IPEndPoint(options.MulticastAddress, options.MulticastPort);

        Console.WriteLine($"[discovery] sending {request.Length}-byte multicast request to {target} " +
                           $"from local port {options.LocalUdpPort} (replay of pcap #4)");
        await udp.SendAsync(request, target, ct);

        var responses = new Dictionary<IPAddress, UdpReceiveResult>();
        var hardDeadline = DateTime.UtcNow + options.DiscoveryTimeout;

        while (true)
        {
            var remaining = hardDeadline - DateTime.UtcNow;
            if (remaining <= TimeSpan.Zero) break;

            // Once at least one server has answered, only wait a short settle
            // window for a second one instead of the full timeout.
            var waitFor = responses.Count == 0
                ? remaining
                : (options.DiscoverySettleTime < remaining ? options.DiscoverySettleTime : remaining);

            using var iterCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            iterCts.CancelAfter(waitFor);

            UdpReceiveResult result;
            try
            {
                result = await udp.ReceiveAsync(iterCts.Token);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                break;
            }

            var addr = result.RemoteEndPoint.Address;
            if (responses.TryAdd(addr, result))
            {
                Console.WriteLine($"[discovery] response: {result.Buffer.Length} bytes from {result.RemoteEndPoint}");
                DumpFrame(result.Buffer);
            }
            else
            {
                Console.WriteLine($"[discovery] additional {result.Buffer.Length}-byte response from " +
                                   $"{result.RemoteEndPoint} (already heard from this server, ignoring)");
            }
        }

        if (responses.Count == 0)
        {
            Console.WriteLine($"[discovery] no response within {options.DiscoveryTimeout.TotalSeconds:0.#}s");
            return null;
        }

        if (responses.Count > 1)
        {
            Console.WriteLine($"[discovery] ERROR: {responses.Count} different servers responded to the discovery " +
                               $"request: {string.Join(", ", responses.Keys)}. There should be exactly one " +
                               "DesignSpooler server on the network - check for a leftover/duplicate instance.");
            return null;
        }

        return responses.Keys.Single();
    }

    private static void DumpFrame(byte[] data)
    {
        if (!Frame.TryParse(data, out var outer))
        {
            Console.WriteLine("  -> does not start with a valid baadbeef frame header!");
            return;
        }

        Console.WriteLine($"  -> outer header OK: msgType={outer.MsgType}, payloadLen={outer.Payload.Length}");

        if (Frame.TryParse(outer.Payload, out var inner))
        {
            Console.WriteLine($"  -> nested header OK: msgType={inner.MsgType}, payloadLen={inner.Payload.Length} " +
                               "(encrypted, contents not decodable)");
        }
    }
}
