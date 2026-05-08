# Description

-----------------------------

# Parsec Performance Data & Analysis

## Connection Success Rate

From log analysis (Jan 14 - May 4, 2026):

* **Total connection attempts**: \~80
* **Successful**: \~70 (87.5%)
* **Failed**: \~10 (12.5%)
* **Failure modes**: Error 6 (3), -6023/-11002 (2), disconnects with bud_write errors (5+)

## Session Duration Patterns

* **ndharmateja (remote)**: 30 min to 2 hr sessions
* **lordlance (local/LAN)**: 1-10 min sessions (frequent connects/disconnects)

## Latency Analysis

| Metric        | Value                      |
|---------------|----------------------------|
| Min latency   | 9ms                        |
| Max latency   | 151ms                      |
| Typical range | 9-14ms (within a session)  |
| Variance      | Very high (9→151ms spikes) |

## FPS Analysis

* Range: 8.5 to 60 FPS
* Local LAN: 9-30 FPS (sluggish, poor for 60 FPS capable machine)
* Remote: 10-60 FPS (variable based on network conditions)

## Bandwidth Usage

* Range: 0.3 to 2.9 Mbps
* Typical: 0.6-1.1 Mbps
* Encoder: h264 at 1024x768 with encoder_bitrate=3

## Network Drops

* Thousands of dropped/lost packets per session
* N counter format: \[dropped/sent/lost\] — values like N:15884/60234/197
* 197 packets lost out of 60,234 sent in one session

## Encoder Issues

* **-15101 error**: AMD encoder init fails on EVERY connection
* Codec used: h264 with AMD hardware encoding
* Format: BGRA input, rgba processing
* Despite error, encoding still functions (video renders)

## Signal Thread Issues

* **-6105 error**: Signaling thread timeout/hang
* Occurs \~30+ times across all logs
* Usually resolves itself after reconnect
* Indicates Parsec signaling server communication hiccups

## Wi-Fi Quality Note

802.11n on 2.4 GHz Channel 5 is a congested band.\
72.2 Mbps link speed is the maximum for 1-stream 802.11n at 20 MHz.\
This bandwidth is shared with all other Wi-Fi devices.

## Performance Conclusion

Performance is **suboptimal** even on LAN due to:

1. 2.4 GHz Wi-Fi congestion
2. AMD encoder init failures causing startup delays
3. High network drop rates (possibly Wi-Fi interference)
4. 1024x768 low resolution streaming

Recommended: Switch to 5 GHz Wi-Fi if router supports it, or use Ethernet for host connections.
