using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using DesignSpoolerClientSimulator;

var options = Options.Parse(args);
if (options is null) return 1;

Console.WriteLine("Embroidery machine client simulator");
Console.WriteLine("Replays the discovery/handshake sequence seen in Stickmaschine-successful-connection.pcapng");
Console.WriteLine();

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

IPAddress? serverIp = options.ServerIp;

// ---- Step 1: UDP multicast discovery -------------------------------------------------
if (serverIp is null)
{
    serverIp = await DiscoveryStep.RunAsync(options, cts.Token);
    if (serverIp is null)
    {
        Console.WriteLine();
        Console.WriteLine("RESULT: FAIL - discovery did not find exactly one server (see above). Aborting.");
        return 1;
    }
}
else
{
    Console.WriteLine($"[discovery] skipped, using --server {serverIp}");
}

Console.WriteLine();

// ---- Step 2: TCP control channel -------------------------------------------------------
var tcpOk = await TcpStep.RunAsync(serverIp, options, cts.Token);

Console.WriteLine();
Console.WriteLine(tcpOk
    ? "RESULT: TCP handshake looks correct (port open, hello echoed, valid RSA key received)."
    : "RESULT: FAIL - TCP handshake did not match the captured behaviour.");

// ---- Step 3: optional UDP heartbeat loop ------------------------------------------------
if (!options.NoHeartbeat)
{
    Console.WriteLine();
    Console.WriteLine($"[heartbeat] replaying discovery bytes to {serverIp}:{options.MulticastPort} every " +
                       $"{options.HeartbeatInterval.TotalSeconds:0.#}s (Ctrl+C to stop)");
    await HeartbeatStep.RunAsync(serverIp, options, cts.Token);
}

return tcpOk ? 0 : 1;
