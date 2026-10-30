# ChatWithAI -- Roadmap

> **Category:** Roadmap
> **Purpose:** Planned and potential future work.

---

## Near-term

- [ ] Wire the Azure OpenAI chat into a Blazor UI page (stream responses via SignalR)
- [ ] Persist conversation history to the database (EF Core `ChatSession` entity)
- [ ] Multi-tenant conversation isolation -- each tenant sees only their own chat history
- [ ] Expose chat as a REST endpoint so other services can call it

## Medium-term

- [ ] Support additional providers (Anthropic, OpenAI direct, Azure AI Foundry)
- [ ] Retrieval-Augmented Generation (RAG) using document embeddings stored in the database
- [ ] Token usage dashboard -- show spend and usage per tenant in the admin UI
- [ ] Plugin hook for custom pre/post-processing of prompts

## Long-term

- [ ] Fine-tuned model management UI
- [ ] Semantic search across uploaded files using vector embeddings
- [ ] Integration with FreeLLM for local/self-hosted model fallback