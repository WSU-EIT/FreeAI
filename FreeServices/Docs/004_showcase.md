# FreeServices — Showcase

> **Category:** Showcase
> **Purpose:** Visual evidence of the project working — screenshots, console output, logs.
> Artifacts live in showcase/ alongside this file.

---

## Service Logs

Log excerpts are captured from the running service and saved here.

For Windows services:
`
Get-EventLog -LogName Application -Source {PROJECT} -Newest 100 | Export-Csv Docs/showcase/logs/eventlog.csv
`

For stdout-logging services, pipe the service output to Docs/showcase/logs/.

> **Status:** No logs captured yet.

---

## Showcase index

| Artifact | Type | Date | Notes |
|----------|------|------|-------|
| *(none yet)* | | | |

---

## How to update this

1. Run the capture process described above
2. Copy artifacts into the appropriate showcase/ subfolder
3. Update the index table above with the artifact path, type, date, and any notes
