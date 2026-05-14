# FreeLLM -- Overview

> **Category:** Overview
> **Purpose:** What this project is, why it exists, and how to get started.

---

## What it is

FreeLLM is a Blazor WebAssembly + ASP.NET Core application for curating local source files into structured, token-balanced LLM prompt packages. Engineers select files from a local directory, apply extension filters and exclusions, preview line/character counts, compose modification instructions, and export a clean prompt split into N balanced chunks -- ready to paste into ChatGPT, Azure OpenAI, Claude, or any other LLM.

The application does not call any LLM API itself; it is a prompt-assembly and clipboard-export tool. The AI logic lives in the engineer's workflow, not in the application.

## Why it exists

Pasting large codebases into LLM chat interfaces is error-prone: context windows overflow silently, important files are forgotten, and the output is hard to reproduce. FreeLLM solves this by making file selection, token estimation, and chunk splitting explicit and repeatable.

## Who it is for

- Software engineers working with LLM-assisted code generation or refactoring
- WSU-EIT developers who iterate frequently with Claude, ChatGPT, or Azure OpenAI on FreeCRM-derived projects

## Quick start

```bash
cd FreeLLM/FreeLLM
dotnet run
```

Navigate to `http://localhost:5102`, enter a local directory path, select files, and copy the assembled chunks.

## Related projects

- [ChatWithAI](../ChatWithAI/README.md) -- Azure OpenAI integration on FreeCRM scaffold
- [FreeManager](../FreeManager/README.md) -- code-generation companion

---

*Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT).***
*Website: https://em.wsu.edu/eit/ | GitHub: https://github.com/WSU-EIT | MIT License*