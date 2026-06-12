# YouTube Authentication Restoration Plan

**Date**: 2026-01-13  
**Scope**: Restore Google OAuth for YouTube Data API (Google Sheets removed from scope)  
**Root Cause**: Commit `6fd7c109` (2026-05-18) gutted `GoogleAuth.cs` from 124 lines to empty stub

---

## Current State Analysis

### What Broke

| File | Before (Working) | After (Broken) |
|------|------------------|----------------|
| `csharp/src/Core/Auth/GoogleAuth.cs` | 124 lines: FileDataStore, SemaphoreSlim, async methods | 3 lines: empty stub |
| `csharp/src/Infrastructure/GoogleCredential.cs` | Did not exist | 84 lines: sync-over-async, no FileDataStore |
| `.env` | Had Google OAuth vars | Missing `GOOGLE_CLIENT_ID`, `GOOGLE_CLIENT_SECRET` |
| `state/google-auth/` | Token cache directory | Does not exist |

### Current Auth Flow (Broken)

```
YouTubeService.CreateAsync() [L31-35]
  → GoogleCredentials.Initializer [L33] (property getter)
    → GoogleCredentials.GetCredential() [L24-63] (SYNC)
      → .Result on AuthorizeAsync() [L60] ⛔ BLOCKS
      → .GetAwaiter().GetResult() on RefreshTokenAsync() [L31] ⛔ BLOCKS
```

**Problems**:
1. No `FileDataStore` — tokens not persisted, browser opens every run
2. Sync-over-async — `.Result` and `.GetAwaiter().GetResult()` block threads
3. No thread safety — no `SemaphoreSlim` for concurrent access
4. Missing env vars — `GOOGLE_CLIENT_ID` and `GOOGLE_CLIENT_SECRET` not in `.env`

---

## Target State: Modern Auth Pattern

### Reference Implementation (Google.Apis.Auth 1.75.0)

Based on official Google documentation and current best practices:

```csharp
public static class GoogleAuth
{
    private static readonly string[] Scopes = [YouTubeService.Scope.YoutubeReadonly];
    private static readonly SemaphoreSlim AuthLock = new(1, 1);
    private static UserCredential? CachedCredential;

    public static async Task<UserCredential> GetCredentialAsync(CancellationToken ct = default)
    {
        await AuthLock.WaitAsync(ct);
        try
        {
            if (CachedCredential is { } cached && !cached.Token.IsStale)
                return cached;

            var authDir = Path.Combine(Paths.StateDirectory, "google-auth");
            var dataStore = new FileDataStore(authDir, fullPath: true);

            CachedCredential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                new ClientSecrets
                {
                    ClientId = Config.GoogleClientId,
                    ClientSecret = Config.GoogleClientSecret,
                },
                Scopes,
                "csharpscripts_user",
                ct,
                dataStore);

            return CachedCredential;
        }
        finally
        {
            AuthLock.Release();
        }
    }

    public static async Task<BaseClientService.Initializer> GetInitializerAsync(
        CancellationToken ct = default)
    {
        var credential = await GetCredentialAsync(ct);
        return new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "Scripts",
        };
    }
}
```

**Key Features**:
- ✅ `FileDataStore` — tokens persist to `state/google-auth/`
- ✅ `SemaphoreSlim` — thread-safe concurrent access
- ✅ Fully async — no `.Result` or `.GetAwaiter().GetResult()`
- ✅ Automatic token refresh — `UserCredential` handles this internally
- ✅ PKCE enabled by default — modern OAuth 2.0 security

---

## Files to Modify

### 1. Restore `GoogleAuth.cs` (Core Auth)

**File**: `csharp/src/Core/Auth/GoogleAuth.cs`  
**Action**: Replace empty stub with modern async implementation

**Changes**:
- Add `using Google.Apis.Auth.OAuth2`, `Google.Apis.Util.Store`, `Google.Apis.YouTube.v3`
- Implement `GetCredentialAsync()` with `FileDataStore` and `SemaphoreSlim`
- Implement `GetInitializerAsync()` factory method
- Remove sync `GetCredential()` method

**Lines**: Full file rewrite (3 lines → ~60 lines)

---

### 2. Update `YouTubeService.cs` (Service Consumer)

**File**: `csharp/src/Services/Sync/YouTube/YouTubeService.cs`  
**Action**: Change `CreateAsync()` to use async auth

**Current** (L31-35):
```csharp
public static Task<YouTubeService> CreateAsync(CancellationToken ct = default)
{
    BaseClientService.Initializer initializer = GoogleCredentials.Initializer;
    return Task.FromResult(new YouTubeService(new YouTubeServiceApi(initializer: initializer)));
}
```

**Target**:
```csharp
public static async Task<YouTubeService> CreateAsync(CancellationToken ct = default)
{
    BaseClientService.Initializer initializer = await GoogleAuth.GetInitializerAsync(ct);
    return new YouTubeService(new YouTubeServiceApi(initializer: initializer));
}
```

**Lines**: L31-35 (5 lines changed)

---

### 3. Delete `GoogleCredential.cs` (Legacy Auth)

**File**: `csharp/src/Infrastructure/GoogleCredential.cs`  
**Action**: Delete file entirely

**Reason**: Superseded by restored `GoogleAuth.cs`. Current implementation has:
- No `FileDataStore` (tokens not persisted)
- Sync-over-async (`.Result`, `.GetAwaiter().GetResult()`)
- No thread safety

**Lines**: Delete 84 lines

---

### 4. Update `.env` (Environment Variables)

**File**: `.env`  
**Action**: Add Google OAuth credentials

**Add**:
```env
# Google OAuth (YouTube Data API)
GOOGLE_CLIENT_ID=your_client_id_here
GOOGLE_CLIENT_SECRET=your_client_secret_here
```

**Lines**: Add 3 lines after current content

**Note**: User must obtain these from Google Cloud Console → APIs & Services → Credentials → OAuth 2.0 Client IDs

---

### 5. Update `Config.cs` (Config Reader)

**File**: `csharp/src/Infrastructure/Config.cs`  
**Action**: No changes needed

**Reason**: Already reads `GOOGLE_CLIENT_ID` and `GOOGLE_CLIENT_SECRET` from environment (L5-6). No modification required.

---

### 6. Delete `Secrets.cs` (Duplicate Config)

**File**: `csharp/src/Core/Auth/Secrets.cs`  
**Action**: Delete file

**Reason**: Duplicate of `Config.cs`. Both read same env vars. `GoogleAuth.cs` will use `Config.cs` (Infrastructure namespace).

**Lines**: Delete 38 lines

---

## Migration Steps

### Phase 1: Preparation

1. **Create recovery branch**
   ```bash
   git branch recovery/auth-restore 6fd7c109^
   ```

2. **Extract working reference** (for comparison)
   ```bash
   git show 6fd7c109^:csharp/src/Core/Auth/GoogleAuth.cs > GoogleAuth.reference.cs
   ```

3. **Verify Google Cloud Console**
   - Navigate to: https://console.cloud.google.com/apis/credentials
   - Confirm OAuth 2.0 Client ID exists for "Desktop app"
   - Copy `Client ID` and `Client Secret`

---

### Phase 2: Implementation

4. **Restore `GoogleAuth.cs`**
   - Replace empty stub with modern async implementation (see Target State above)
   - Use `Config.GoogleClientId` and `Config.GoogleClientSecret`
   - Use `Paths.StateDirectory` for token storage path

5. **Update `YouTubeService.cs`**
   - Change `CreateAsync()` from sync to async
   - Replace `GoogleCredentials.Initializer` with `await GoogleAuth.GetInitializerAsync(ct)`

6. **Delete `GoogleCredential.cs`**
   - Remove file from `csharp/src/Infrastructure/`
   - Remove from project if explicitly referenced (check `.csproj`)

7. **Delete `Secrets.cs`**
   - Remove file from `csharp/src/Core/Auth/`
   - Verify no other files reference `Secrets.*` (grep first)

---

### Phase 3: Configuration

8. **Update `.env`**
   - Add `GOOGLE_CLIENT_ID` and `GOOGLE_CLIENT_SECRET`
   - Use values from Google Cloud Console

9. **Verify `Config.cs`**
   - Confirm it reads the env vars correctly (L5-6)
   - No changes expected

---

### Phase 4: Verification

10. **Build project**
    ```bash
    dotnet build csharp/Scripts.csproj
    ```
    - Expected: 0 errors, 0 warnings

11. **Run YouTube sync (first time — browser opens)**
    ```bash
    dotnet run --project csharp/Scripts.csproj -- sync yt
    ```
    - Expected: Browser opens for Google OAuth
    - Expected: User authenticates
    - Expected: Token saved to `state/google-auth/`
    - Expected: Sync completes successfully

12. **Verify token persistence**
    ```bash
    ls state/google-auth/
    ```
    - Expected: File named `Google.Apis.Auth.OAuth2.Responses.TokenResponse-csharpscripts_user`

13. **Run YouTube sync (second time — no browser)**
    ```bash
    dotnet run --project csharp/Scripts.csproj -- sync yt
    ```
    - Expected: NO browser opens
    - Expected: Token loaded from disk
    - Expected: Sync completes successfully

14. **Test token refresh** (wait 1 hour or force expiry)
    - Expected: Token auto-refreshes without browser prompt
    - Expected: Sync continues successfully

---

## Call Chain (After Migration)

```
CLI: SyncYouTubeCommand.ExecuteAsync()
  → YouTubePlaylistOrchestrator.CreateAsync()
    → YouTubeService.CreateAsync() [ASYNC]
      → await GoogleAuth.GetInitializerAsync() [ASYNC]
        → await GoogleAuth.GetCredentialAsync() [ASYNC]
          → FileDataStore (load token from disk)
          → GoogleWebAuthorizationBroker.AuthorizeAsync() [ASYNC]
            → Browser opens (first time only)
            → Token saved to state/google-auth/
      → new YouTubeServiceApi(initializer)
    → orchestrator.ExecuteAsync() [ASYNC]
      → YouTube API calls [ASYNC]
```

**All async, no blocking, tokens persisted.**

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Google Cloud Console credentials expired | Medium | High | User must verify credentials exist and are active |
| Token storage path changed | Low | Medium | Use `Paths.StateDirectory` (already defined) |
| Other files reference `GoogleCredentials` | Low | High | Grep for `GoogleCredentials` before deleting (already done: only YouTubeService.cs L33) |
| `Config.cs` env var names changed | Low | Medium | Verify L5-6 match `.env` keys |
| Build fails due to missing references | Low | High | Run `dotnet build` after each file change |

---

## Out of Scope

- ❌ Google Sheets auth (removed per user request)
- ❌ Last.fm auth (separate system, not broken)
- ❌ Python auth (`python/toolkit/lastfm.py`) — separate codebase
- ❌ Old CLI commands (`SyncCommands.cs`, `CleanCommands.cs`) — superseded by new `Sync/` commands
- ❌ `Resilience.cs` sync wrappers — not blocking YouTube sync

---

## Success Criteria

- [ ] `dotnet build` succeeds with 0 errors
- [ ] First `sync yt` run opens browser, completes sync
- [ ] Token file created in `state/google-auth/`
- [ ] Second `sync yt` run does NOT open browser
- [ ] Token auto-refreshes after expiry (test after 1 hour)
- [ ] No `.Result` or `.GetAwaiter().GetResult()` in auth code path
- [ ] `GoogleCredential.cs` deleted
- [ ] `Secrets.cs` deleted
- [ ] `.env` contains `GOOGLE_CLIENT_ID` and `GOOGLE_CLIENT_SECRET`

---

## References

- **Working version**: `git show 6fd7c109^:csharp/src/Core/Auth/GoogleAuth.cs`
- **Official docs**: https://github.com/googleapis/google-api-dotnet-client
- **Google.Apis.Auth**: Version 1.75.0 (latest)
- **FileDataStore**: Built-in token persistence
- **PKCE**: Enabled by default (OAuth 2.0 security best practice)
