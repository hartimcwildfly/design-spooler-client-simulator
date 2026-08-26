using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using DesignSpoolerClientSimulator.Resources;

namespace DesignSpoolerClientSimulator;

public static class TcpStep
{
    /// <summary>
    /// Connects to the server's TCP control port, replays the deterministic
    /// "hello" message (pcap #10: version=1, command=3) and checks it comes
    /// back unchanged (pcap #12), then reads the following message and
    /// verifies it is a well-formed RSA public key (pcap #14).
    ///
    /// Anything past this point in the real capture (pcap #15 onward) is an
    /// RSA-encrypted, session-specific payload whose plaintext/algorithm is
    /// unknown, so it is intentionally not replayed here - doing so would just
    /// send stale ciphertext that no server could ever decrypt correctly.
    /// </summary>
    public static async Task<bool> RunAsync(IPAddress serverIp, Options options, CancellationToken ct)
    {
        using var tcp = new TcpClient();

        Console.WriteLine(string.Format(Messages.TcpConnecting, serverIp, options.TcpPort));
        try
        {
            using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            connectCts.CancelAfter(options.TcpTimeout);
            await tcp.ConnectAsync(serverIp, options.TcpPort, connectCts.Token);
        }
        catch (Exception ex) when (ex is SocketException or OperationCanceledException)
        {
            Console.WriteLine(string.Format(Messages.TcpConnectFail, ex.Message));
            return false;
        }
        Console.WriteLine(Messages.TcpConnected);

        await using var stream = tcp.GetStream();

        var hello = Frame.Build(0, [0, 0, 0, 1, 0, 0, 0, 3]);
        Console.WriteLine(string.Format(Messages.TcpSendingHello, hello.Length));
        await stream.WriteAsync(hello, ct);

        var helloReply = await ReadFrameRawAsync(stream, options, ct);
        if (helloReply is null)
        {
            Console.WriteLine(Messages.TcpNoHelloReply);
            return false;
        }

        if (helloReply.AsSpan().SequenceEqual(hello))
        {
            Console.WriteLine(Messages.TcpHelloEchoOk);
        }
        else
        {
            Console.WriteLine(string.Format(Messages.TcpHelloDiffers, helloReply.Length, Convert.ToHexString(helloReply)));
            Console.WriteLine(Messages.TcpHelloDiffersNote);
        }

        var keyMsg = await ReadFrameRawAsync(stream, options, ct);
        if (keyMsg is null)
        {
            Console.WriteLine(Messages.TcpNoKeyMsg);
            return false;
        }

        if (!Frame.TryParse(keyMsg, out var keyFrame))
        {
            Console.WriteLine(Messages.TcpKeyInvalidFrame);
            return false;
        }

        try
        {
            using var rsa = RSA.Create();
            rsa.ImportRSAPublicKey(keyFrame.Payload, out var bytesRead);
            var parameters = rsa.ExportParameters(false);
            var exponent = new System.Numerics.BigInteger(parameters.Exponent!, isUnsigned: true, isBigEndian: true);
            Console.WriteLine(string.Format(Messages.TcpKeyOk, rsa.KeySize, exponent, bytesRead, keyFrame.Payload.Length));
        }
        catch (CryptographicException ex)
        {
            Console.WriteLine(string.Format(Messages.TcpKeyParseFail, ex.Message));
            Console.WriteLine(string.Format(Messages.TcpKeyPayload, keyFrame.Payload.Length, Convert.ToHexString(keyFrame.Payload)));
            return false;
        }

        return true;
    }

    private static async Task<byte[]?> ReadFrameRawAsync(NetworkStream stream, Options options, CancellationToken ct)
    {
        using var readCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        readCts.CancelAfter(options.TcpTimeout);

        var header = new byte[Frame.HeaderLen];
        try
        {
            await ReadExactAsync(stream, header, readCts.Token);
        }
        catch (Exception ex) when (ex is IOException or OperationCanceledException && !ct.IsCancellationRequested)
        {
            return null;
        }

        var length = BinaryPrimitives.ReadUInt32BigEndian(header.AsSpan(12, 4));
        var payload = new byte[length];
        try
        {
            await ReadExactAsync(stream, payload, readCts.Token);
        }
        catch (Exception ex) when (ex is IOException or OperationCanceledException && !ct.IsCancellationRequested)
        {
            return null;
        }

        return [.. header, .. payload];
    }

    private static async Task ReadExactAsync(NetworkStream stream, byte[] buffer, CancellationToken ct)
    {
        var offset = 0;
        while (offset < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(offset), ct);
            if (read == 0) throw new IOException("connection closed by remote host");
            offset += read;
        }
    }
}
