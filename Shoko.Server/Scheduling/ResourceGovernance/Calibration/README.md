# Resource Limit Calibration

Calibration is passive. It never tries to discover provider limits by deliberately exceeding them.

```text
Provider response / error
    |
    v
Calibration observation
    |
    +-- success
    +-- throttle / backoff signal
    +-- ban signal
    |
    v
Provider limit profile
    |
    v
Resource limit delay
    |
    v
Dispatch gate / rate limiter
```

For AniDB, calibration only makes dispatch more conservative when AniDB tells us to slow down. Normal documented limits remain the floor, and calibration adds temporary cooling periods on top.

## AniDB Signals

```text
HTTP success
  -> record success

HTTP ban XML / HTTP 429 / HTTP 503
  -> record ban or throttle signal

UDP success
  -> record success

UDP BANNED
  -> record ban signal

UDP SERVER_BUSY / TIMEOUT_DELAY_AND_RESUBMIT / overload backoff
  -> record throttle signal
```

The current implementation is in-memory. A future persistent profile can be added here without changing individual jobs.
