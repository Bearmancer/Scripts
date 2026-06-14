## Raw DTO split evidence

- Before implementation, the targeted contract test path did not have a raw DTO contract and `dotnet test` was not able to execute it cleanly in this environment.
- Introduced `YouTubeVideoRaw` for raw persistence and kept translation-only fields on derived `YouTubeVideo`.
- `StateManager.SavePlaylistCache` now serializes raw DTOs only; `LoadPlaylistCache` deserializes raw DTOs only.
- Contract check passed with `dotnet run --project csharp/tests/Scripts.Tests/Scripts.Tests.csproj -- --treenode-filter "/*/*/YouTubeRawDtoContractTests/*"`.
