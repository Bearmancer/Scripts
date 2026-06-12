## 2026-06-12 Task 1
- pgtui config is `C:\Users\Lance\AppData\Roaming\pgtui\config.toml` and accepts `dbs = ["postgresql://lance:lance@localhost:5432/pg_db"]`.
- Clean-state gate script `powershell\tests\Assert-YoutubeStateClean.ps1` should assert no playlist/deleted JSON remains under `state\youtube\` and `sync.json` has no `PlaylistSnapshots`.
- TUnit command-line in this repo uses `--treenode-filter`, not `--filter`.
## 2026-06-12 Task 2
- `DisableTestingPlatformServerCapability` is a real MSBuild opt-out for Microsoft.Testing.Platform Test Explorer server mode. In `Microsoft.Testing.Platform.targets`, `ProjectCapability Include="TestingPlatformServer"` and `TestContainer` are only added when `DisableTestingPlatformServerCapability != 'true'` and `IsTestingPlatformApplication == 'true'`.
  - Source: https://github.com/microsoft/testfx/blob/0b4aaa7be5bc4b94389c28ccfe3fcffbcf1d1375/src/Platform/Microsoft.Testing.Platform/buildMultiTargeting/Microsoft.Testing.Platform.targets#L8-L17
  - Public example: https://github.com/xunit/xunit.net/blob/main/site/docs/getting-started/v3/microsoft-testing-platform.md
- Official Microsoft docs do not document a standalone CLI switch to disable the raw MTP server handshake. For MSTest, the documented escape hatch is `UseVSTest=true`; `MSTest.Sdk` also defaults `EnableMSTestRunner` and `TestingPlatformDotnetTestSupport` to `true`.
  - Source: https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-mstest-sdk
  - Source: https://learn.microsoft.com/en-us/dotnet/core/project-sdk/msbuild-props#vstest%E2%80%93related-properties
