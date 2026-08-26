# DesignSpoolerClientSimulator

A C# console app that simulates a Tajima embroidery machine's network
client, so a Pulse DesignSpooler server implementation can be tested without
the physical machine. It replays the discovery/handshake sequence recorded
in `Stickmaschine-successful-connection.pcapng` (filtered on MAC
`00:d0:c9:aa:8a:69`, the embroidery machine) and reports whether the server
under test behaves the same way.

## What it does

1. **UDP discovery** — sends the machine's original multicast discovery
   packet (byte-for-byte replay of capture packet #4) to `234.5.6.7:4564`
   from local port 3001, and waits for a unicast reply (like #6). Reports
   whether a reply arrived and whether it has the expected frame structure.
   Exactly one server is expected to answer: after the first reply, it keeps
   listening for a short settle window (`--discovery-settle`, default 0.75s)
   in case a second one responds too, and treats that as an error — the real
   embroidery machine has no way to choose between two servers, so more than
   one on the network is a misconfiguration, not something to tolerate.
2. **TCP handshake** — connects to the responding server's IP on port
   `9050` (like #7–9), sends the fixed 8-byte hello message
   (`version=1, command=3`, replay of #10), and checks that the server
   echoes it back unchanged (as it did in #12).
3. **RSA public key check** — reads the server's next message and verifies
   it parses as a valid RSA public key (PKCS#1 DER), matching the shape of
   capture packet #14 (512-bit modulus, exponent 65537 in the reference
   capture).
4. **Heartbeat loop** — afterwards, repeatedly sends the same discovery
   bytes as a unicast probe to the server every ~2 seconds (matching the
   real client's behaviour from packet #20 onward), so you can leave it
   running and watch whether the server's UDP listener keeps responding.

Every step prints a plain PASS/FAIL-style line to the console.

## Protocol notes

All messages share a 32-byte frame header:

```
uint32 magic     = 0xBAADBEEF
uint32 headerLen = 0x20 (32)
uint32 msgType   = 0 (plain frame) or 1 (nested "encrypted record" frame)
uint32 length    = bytes following this header
byte[16] reserved (always zero in the capture)
```

Some messages (hello, RSA key) are a single header wrapping raw payload.
Others (UDP discovery/heartbeat, TCP application data) wrap a second header
around what looks like ciphertext — see Limitations.

## About the capture file

`Stickmaschine-successful-connection.pcapng` has been filtered down from the
original ~1750-packet capture to 54 frames: every frame with the embroidery
machine's MAC (`00:d0:c9:aa:8a:69`) as either the Ethernet source or
destination, and nothing else. Since the server's replies are addressed
directly to that MAC on this network, this still captures the full two-way
conversation (discovery reply, TCP handshake, heartbeats). It also drops
everything that isn't: the server's own unrelated traffic (SLP announcements,
ARP for other hosts, other multicast groups) never has the client's MAC as
source or destination, so none of it survives this filter. Packet numbers
cited throughout this README and the source code (`#4`, `#10`, ...) refer to
this filtered file, not the original.

The one side effect: the embroidery machine's DHCP request/ack (`#0`–`#3`)
is included, since those frames do carry its MAC. That's the machine
acquiring its own IP address before it starts discovery — left in since it's
still solely the embroidery machine's own traffic, not a third party's.

## Usage

```
dotnet run                              # full flow: discover, connect, handshake, heartbeat
dotnet run -- --server 192.168.x.x      # skip discovery, connect straight to a known IP
dotnet run -- --no-heartbeat            # stop after the handshake instead of looping
dotnet run -- --help                    # all options (ports, timeouts, multicast address, ...)
```

Targets `net10.0`. Requires a server actually listening on the multicast
discovery port and TCP 9050 to get past step 1/2 — against nothing, it fails
fast with a clear timeout/connection-refused message.

### Prebuilt binaries

The GitHub Actions workflow (`.github/workflows/dotnet.yml`) builds a
self-contained, single-file executable for every push/PR to `main`, for
win/linux/linux-musl/osx × x64/arm64. Download the artifact matching your
platform from the workflow run and execute it directly — no .NET runtime
required on the target machine.

For Windows, the workflow also builds a `.exe` installer (via
[Inno Setup](https://jrsoftware.org/isinfo.php), see `installer/`) for
x64/arm64, published as the `DesignSpoolerClientSimulator-Setup-win-*`
artifacts. It installs to `Program Files`, adds a Start Menu entry and a
normal "Programs and Features" uninstall entry, and supports silent
installs (`Setup.exe /VERYSILENT /SUPPRESSMSGBOXES`) for software
distribution tools that expect an installer rather than a raw binary.

## Limitations

- **Only the outer framing is understood, not the encryption.** The payload
  bytes inside the nested (`msgType=1`) frames — the discovery request, the
  heartbeat, and the later TCP application messages — are opaque ciphertext.
  The algorithm and key are unknown, so this tool cannot regenerate them; it
  only **replays the exact bytes captured** from the real machine. A server
  with replay protection may legitimately reject them, which would show up
  here as a false failure.
- **The RSA-encrypted handshake response is not replayed.** Right after the
  server sends its public key, the real client sends back a 1280-byte
  RSA-encrypted blob (capture packet #15) — almost certainly a session
  key or similar, encrypted under that specific session's public key. This
  tool does not send anything back after reading the key, because replaying
  the old ciphertext from the capture would be meaningless (it was encrypted
  for a different key pair) and we don't know how to construct a new one
  without the algorithm. As a result, everything the real protocol does
  *after* the key exchange (encrypted TCP session, and probably the ongoing
  encrypted UDP traffic) is out of scope for this tool.
- **The heartbeat is a liveness probe, not a protocol-accurate replay.** It
  resends the discovery bytes rather than a real, growing heartbeat payload,
  since that payload is also encrypted and session-specific. It's only
  useful for checking "is the server's UDP listener still up and replying
  to something", not for validating heartbeat semantics.
- **Fixed local ports.** The tool binds local UDP port 3001 and connects
  from an OS-assigned TCP port (the capture used 3002, but nothing in the
  protocol appeared to depend on that specific value). If port 3001 is
  already in use on your machine, discovery will fail to bind.
- Only one capture was available for reverse engineering, of a single
  successful session. Behaviour that varies between sessions or devices
  (e.g. different discovery payload contents) couldn't be verified.
