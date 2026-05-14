# FreeLLM -- Roadmap

> **Category:** Roadmap
> **Purpose:** Planned and potential future work.

---

## Near-term

- [ ] Token counting in the browser (WASM-based BPE tokenizer) so chunk boundaries match actual context limits
- [ ] Saved file selection sets -- remember which files a user typically includes for a given project
- [ ] Fix boot issue when ASPNETCORE_URLS env var is not set (currently binds to random port)

## Medium-term

- [ ] Azure OpenAI integration -- send the assembled prompt directly from the UI and stream the response back
- [ ] Git diff mode -- pre-select only files changed since a given commit
- [ ] `.gitignore`-aware file exclusion

## Long-term

- [ ] Support remote directories (SSH / SMB mount) so the tool is not limited to the server''s local filesystem
- [ ] Session history -- save and replay previous prompts with the same file selection