# FreeTools — Workspace Documentation

This folder previously held five unfilled documentation templates (`000_overview.md` through
`004_showcase.md`). They contained only `<!-- TODO -->` placeholders and were linked from nothing, so
they have been removed rather than left to look like documentation that exists.

The real documentation lives in three places:

| Location | Contents |
|----------|----------|
| **[../README.md](../README.md)** | Workspace overview — what is here, solution status, the FreeCRM extension pattern |
| **[../FreeTools/README.md](../FreeTools/README.md)** | The tool suite: pipeline diagram, per-tool reference, environment variables, known rough edges |
| **[../FreeTools/Docs/](../FreeTools/Docs/)** | Suite-level detail — architecture, style guide, security notes, and the `FreeTools.Core` API reference |

## Generated output

The `Docs.csproj` in this folder is a content-only project. It carries no compilable code; it exists
to include `showcase/**` in build output.

Pipeline runs are written to `../FreeTools/Docs/runs/{Project}/{Branch}/latest/` — not here.

## GuidesV2

[`../FreeTools/Docs/GuidesV2/`](../FreeTools/Docs/GuidesV2/) is a separate library of 71 long-form
guides on FreeCRM development practice, plus the meeting and briefing transcripts that produced them.
It is reference material about *building FreeCRM applications*, not about the FreeTools CLIs, and is
maintained independently.
