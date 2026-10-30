# FreeServicesHub -- Roadmap

> **Category:** Roadmap
> **Purpose:** Planned and potential future work.

---

## Near-term

- [ ] Agent installer UI -- register and install the Windows Service from the hub web UI
- [ ] Alert thresholds -- notify when CPU > N% or disk < N GB for more than M minutes
- [ ] Historical telemetry charts -- graph CPU/RAM over time using stored heartbeats

## Medium-term

- [ ] Linux agent support (replace WMI CPU query with /proc/stat)
- [ ] Multi-hub federation -- one hub aggregating data from regional sub-hubs
- [ ] Windows Service management -- start/stop/restart services on remote agents from the hub

## Long-term

- [ ] Anomaly detection -- flag unusual resource consumption patterns
- [ ] Integration with FreeA11yChecker -- correlate scan times with server load