# Implementation Plan: Remaining Tasks

**Created**: 2024-12-24 10:11  
**Updated**: 2024-12-24 10:35  
**Total Items**: 20 → **7 remaining**  
**Completed This Session**: 13

---

## ✅ COMPLETED THIS SESSION

### C# Cleanup
- [x] **Item 58**: Removed all comments from C# source files
- [x] **Item 54**: Verified Console.cs enforcement (no direct AnsiConsole calls)

### PowerShell/Whisper
- [x] **Item 15**: Verified `whisp` alias already uses correct defaults
- [x] **Items 16-18, 23-25**: Verified Invoke-Whisper implementation (legend, model download notes added)
- [x] **Item 22**: Profiled PowerShell load time (~1.3s with profile, ~315ms without)

### Documentation
- [x] **Item 53**: Created `.gemini/named_arguments_convention.md`

### Validation
- [x] **Items 62-64**: Verified sync terminal behavior and exit codes

### Python Cleanup
- [x] Removed unused `os` import from `dump_legacy_sigs.py`
- [x] Removed `#region` markers from `python/toolkit/lastfm.py`

---

## ⏳ REMAINING (7 items)

### Cannot Change (Upstream Dependencies)
- [ ] **19**: tqdm ETA/Elapsed format (controlled by whisper-ctranslate2)
- [ ] **20**: tqdm rate unit format (controlled by whisper-ctranslate2)

### Needs Implementation
- [ ] **28**: Complete missing fields implementation
- [ ] **29**: Improve missing fields progress display
- [ ] **52**: Named arguments migration (apply to existing code)

### Awaiting Decision
- [ ] **61**: Python scrobbler integration architecture

---

## Session Statistics

| Metric     | Before | After   |
| ---------- | ------ | ------- |
| Total Done | 78     | **91**  |
| Remaining  | 20     | **7**   |
| Progress   | 80%    | **93%** |
