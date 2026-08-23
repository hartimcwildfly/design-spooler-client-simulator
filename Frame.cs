using System.Buffers.Binary;

namespace DesignSpoolerClientSimulator;

/// <summary>
/// Codec for the "baadbeef" frame header observed on both the UDP discovery
/// channel and the TCP control channel in the capture:
///
///   uint32 magic       = 0xBAADBEEF
///   uint32 headerLen    = 0x20 (32, always constant in the capture)
///   uint32 msgType      = 0 for a plain/control frame, 1 for a nested
///                          "encrypted record" frame that itself starts
///                          with another such header
///   uint32 length       = number of bytes following this header
///   byte[16] reserved   = always zero in the capture
///
/// Some messages (the version/command hello, the RSA public key) consist of
/// a single header wrapping raw payload bytes. Others (the UDP discovery
/// request/response, the TCP application-data records) wrap a *second*
/// header (msgType 1) before the actual ciphertext. This class only builds
/// and parses the outer, structural framing - it does not know the
/// proprietary encryption used for the inner payloads.
/// </summary>
public static class Frame
{
    public const uint Magic = 0xBAADBEEF;
    public const uint HeaderLen = 32;

    public static byte[] Build(uint msgType, byte[] payload)
    {
        var buffer = new byte[HeaderLen + payload.Length];
        var span = buffer.AsSpan();
        BinaryPrimitives.WriteUInt32BigEndian(span[0..4], Magic);
        BinaryPrimitives.WriteUInt32BigEndian(span[4..8], HeaderLen);
        BinaryPrimitives.WriteUInt32BigEndian(span[8..12], msgType);
        BinaryPrimitives.WriteUInt32BigEndian(span[12..16], (uint)payload.Length);
        // bytes 16..32 stay zero (reserved)
        payload.CopyTo(span[32..]);
        return buffer;
    }

    public static bool TryParse(ReadOnlySpan<byte> data, out ParsedFrame frame)
    {
        frame = default;
        if (data.Length < HeaderLen) return false;

        var magic = BinaryPrimitives.ReadUInt32BigEndian(data[0..4]);
        if (magic != Magic) return false;

        var headerLen = BinaryPrimitives.ReadUInt32BigEndian(data[4..8]);
        var msgType = BinaryPrimitives.ReadUInt32BigEndian(data[8..12]);
        var length = BinaryPrimitives.ReadUInt32BigEndian(data[12..16]);

        if (headerLen != HeaderLen) return false;
        if (data.Length < HeaderLen + length) return false;

        frame = new ParsedFrame(msgType, data.Slice((int)HeaderLen, (int)length).ToArray());
        return true;
    }
}

public readonly record struct ParsedFrame(uint MsgType, byte[] Payload);
