## 2026-06-12 Task 1
- `dotnet test` with `--filter` is not valid for TUnit in this repo. Use `--treenode-filter` or `dotnet run` for targeted execution.
- LSP server was missing until `csharp-ls` was installed.
## 2026-06-12 Task 2
- I did not find a Microsoft Learn-documented public env var/runsettings/CLI switch that disables the internal MTP `--server` / `--dotnet-test-pipe` handshake path itself. The source treats those options as hidden/internal and expects them to be supplied together by `dotnet test`.
  - Source: https://github.com/microsoft/testfx/blob/0b4aaa7be5bc4b94389c28ccfe3fcffbcf1d1375/src/Platform/Microsoft.Testing.Platform/CommandLine/PlatformCommandLineProvider.cs#L39-L44
  - Source: https://github.com/microsoft/testfx/blob/0b4aaa7be5bc4b94389c28ccfe3fcffbcf1d1375/src/Platform/Microsoft.Testing.Platform/CommandLine/PlatformCommandLineProvider.cs#L209-L218
- If the failure is a named-pipe timeout rather than a hard disable request, `TESTINGPLATFORM_DEFAULT_HANG_TIMEOUT` is the documented knob for the test host controller/test host pipe connection timeout; `TESTINGPLATFORM_LAUNCH_ATTACH_DEBUGGER` is useful for early-attach debugging of startup-time handshake failures.
  - Source: https://learn.microsoft.com/en-us/dotnet/core/testing/microsoft-testing-platform-config
