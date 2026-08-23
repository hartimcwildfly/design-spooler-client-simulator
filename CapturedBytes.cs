namespace DesignSpoolerClientSimulator;

/// <summary>
/// Raw bytes lifted verbatim from Stickmaschine-successful-connection.pcapng
/// (packet #4, the client's original multicast discovery request).
///
/// The payload inside the frame is a proprietary, machine-specific encrypted
/// blob (likely containing a serial number / identity token) whose algorithm
/// is not known, so it cannot be regenerated - it is replayed byte-for-byte.
/// A server that implements replay protection on this payload may reject it;
/// that is a legitimate reason for the discovery step below to fail even
/// though the server is otherwise healthy. Everything else in this tool
/// (the frame header, the hello handshake, the RSA key parsing) is
/// constructed/parsed generically rather than replayed.
/// </summary>
public static class CapturedBytes
{
    public const string DiscoveryRequestHex =
        "baadbeef00000020000000000000014600000000000000000000000000000000" +
        "baadbeef00000020000000010000012600000000000000000000000000000000" +
        "a722b182592f7c670d216a7b7bee7c53c2f756488d3172a3f1dada8829357dcdc4" +
        "e8ea5e363506f872ace9f7d36760a172978f73452b666aa6ed774a17cc87cad0f3" +
        "a699f70bebae2886ed2a94fd2ec068cc97e661095f1d805b59580a2132d308d5a7" +
        "fe65e85f470d4d4395873500cfc3aad56c56b30631609b3e586950f16e84edc624" +
        "8aa4f8af351e4ec496e893d6d8b2a38934ec17c8956e5c4a0fa45e950d36bf251d" +
        "ff7c14557ecaf393cf9ab2847e9500381e87149c2e30a30849e8713b8e66d2f48b" +
        "1ec7b21eaac5022ac94d139799411ad5e83de8d03529b937e623c9d8631107f6ba" +
        "d783c737bd503a29c114abedf52e21abb357a7421a6874346987c801b07313fd5" +
        "8cbe03bb4665081c528548ed414b60cd77d17e370beb6ff97d3e9e3c7f562";

    public static byte[] DiscoveryRequest => Convert.FromHexString(DiscoveryRequestHex);
}
