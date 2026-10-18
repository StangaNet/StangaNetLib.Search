# StangaNetLib.Search — Copilot instructions

- Search abstractions: `ISearchService<TDocument>`, `ISearchIndexManager`; in-memory BM25 backend for tests/dev.
- Prefer `Result` / `Result<T>` and `Guard` from `StangaNetLib.Core` — do not reintroduce local Result/Guard types.
- Keep public APIs documented (`///`) and options-driven via `SearchSettings`.
- DI: `AddStangaNetLibSearch<TDocument>` (index name or configuration).
- Elasticsearch / Meilisearch are separate packages (`InternalsVisibleTo`); do not embed provider clients here.
- Match existing naming, file-scoped namespaces, and EditorConfig / Roslyn conventions.
- Tests use xUnit v3 + FluentAssertions; cover both `net8.0` and `net9.0` when changing shared behavior.
- Never reintroduce Microsoft.Extensions.* 10.x; pin Core 1.0.0.
