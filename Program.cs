using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using DesignSpoolerClientSimulator;
using DesignSpoolerClientSimulator.Resources;

var options = Options.Parse(args);
if (options is null)
{
    Console.WriteLine();
    Console.WriteLine(Messages.PressAnyKeyToExit);
    Console.ReadKey();
    return 1;
}

Console.WriteLine(Messages.AppTitle);
Console.WriteLine(Messages.AppSubtitle);
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
        Console.WriteLine(Messages.DiscoveryFailAbort);
        Console.WriteLine();
        Console.WriteLine(Messages.PressAnyKeyToExit);
        Console.ReadKey();
        return 1;
    }
}
else
{
    Console.WriteLine(string.Format(Messages.DiscoverySkipped, serverIp));
}

Console.WriteLine();

// ---- Step 2: TCP control channel -------------------------------------------------------
var tcpOk = await TcpStep.RunAsync(serverIp, options, cts.Token);

Console.WriteLine();
Console.WriteLine(tcpOk ? Messages.TcpResultOk : Messages.TcpResultFail);

// ---- Step 3: optional UDP heartbeat loop ------------------------------------------------
if (!options.NoHeartbeat)
{
    Console.WriteLine();
    Console.WriteLine(string.Format(Messages.HeartbeatStarting, serverIp, options.MulticastPort,
        options.HeartbeatInterval.TotalSeconds.ToString("0.#")));
    await HeartbeatStep.RunAsync(serverIp, options, cts.Token);
}
else
{
    Console.WriteLine();
    Console.WriteLine(Messages.PressAnyKeyToExit);
    Console.ReadKey();
}

return tcpOk ? 0 : 1;
