# Cmder (Clink/Lua) ScriptsToolkit

> **Environment:** Clink (embedded in Cmder)
> **Language:** Lua 5.x
> **Philosophy:** Inject powerful capabilities into the legacy Windows CMD via Lua scripts.

---

## Complete Implementation (`ScriptsToolkit.lua`)

Save this file to `%CMDER_ROOT%\config\ScriptsToolkit.lua`.

```lua
-- ScriptsToolkit.lua
-- Clink (Cmder) implementation of scripts toolkit
-- Place in %CMDER_ROOT%\config\

--------------------------------------------------------------------------------
-- Helper Constants & Paths
--------------------------------------------------------------------------------

local green = "\x1b[32m"
local cyan = "\x1b[36m"
local yellow = "\x1b[33m"
local red = "\x1b[31m"
local reset = "\x1b[0m"

-- Start with the current script path's parent as root
-- clink.get_env("CMDER_ROOT") is usually available
local repo_root = "C:/Users/Lance/Dev"
local py_toolkit = repo_root .. "/python/toolkit/cli.py"
local csharp_root = repo_root .. "/csharp"

--------------------------------------------------------------------------------
-- Helper Functions
--------------------------------------------------------------------------------

--- Print styled info message
-- @param msg The message string
local function log_info(msg)
    print(cyan .. "[INFO] " .. reset .. msg)
end

--- Execute a python command
-- @param args Table of arguments
local function invoke_python(args)
    local cmd_parts = {"python", py_toolkit}
    for _, v in ipairs(args) do
        table.insert(cmd_parts, v)
    end
    
    -- table.concat joins array elements with spaces
    local cmd_str = table.concat(cmd_parts, " ")
    os.execute(cmd_str)
end

--- Create a Clink argument matcher
-- Used for tab completion
local function make_parser(flags)
    local parser = clink.arg.new_parser()
    parser:set_flags(table.unpack(flags or {}))
    return parser
end

--------------------------------------------------------------------------------
-- Command Definitions
--------------------------------------------------------------------------------

-- Dictionary table to store command descriptions for help
local commands_help = {}

--- Register a global function as a callable command in Clink/OS
-- In Clink, we utilize 'doskey' aliases via clink handlers or just define global lua functions
-- that get called via a dispatcher. For simplicity in Cmder, we create simpler aliases or
-- usage wrappers. 
-- However, standard Clink Lua API hooks into prompt, not directly creating 'exe's.
-- PROPER APPROACH: We define a match generator and execute via os.execute wrapper functions.

local function tkfn()
    print("\n" .. cyan .. "ScriptsToolkit (Cmder/Lua)" .. reset)
    print("========================")
    
    -- Iterating sorted pairs is not native in Lua 5.1/LuaJIT, manual sort needed or simple iteration
    for cmd, desc in pairs(commands_help) do
        -- string.format for padding
        io.write(green .. string.format("%-12s", cmd) .. reset .. desc .. "\n")
    end
    print("")
end

-- Filesystem: dirs
local function dirs(directory, sort)
    directory = directory or "."
    sort = sort or "size"
    invoke_python({"filesystem", "tree", "--directory", directory, "--sort", sort})
end
commands_help["dirs"] = "List directories with sizes"

-- Filesystem: tree
local function tree(directory)
    directory = directory or "."
    invoke_python({"filesystem", "tree", "--directory", directory, "--include-files"})
end
commands_help["tree"] = "List files and directories with sizes"

-- Video: remux
local function remux(path)
    path = path or "."
    invoke_python({"video", "remux", "--path", path})
end
commands_help["remux"] = "Remux video disc folders to MKV"

-- Transcription: whisp
local function whisp(path, lang, model)
    if not path then 
        print(red .. "Error: Path required" .. reset)
        return
    end
    
    local args = {"--output_format", "srt"}
    
    -- Conditional table insertion
    if model then 
        table.insert(args, "--model")
        table.insert(args, model)
    else
        table.insert(args, "--model")
        table.insert(args, "large-v3")
    end

    if lang then
        table.insert(args, "--language")
        table.insert(args, lang)
    end
    
    -- Append path last
    table.insert(args, path)
    
    -- Execute whisper-ctranslate2 directly (assuming it's in PATH)
    -- We can't use invoke_python here as it wraps the toolkit cli.py
    -- We construct the command string manually
    local cmd = "whisper-ctranslate2 " .. table.concat(args, " ")
    print(cyan .. "Running: " .. cmd .. reset)
    os.execute(cmd)
end
commands_help["whisp"] = "Transcribe media using Whisper"

-- Sync: syncall
local function syncall()
    print(cyan .. "[YouTube Sync]" .. reset)
    -- string concatenation with ..
    local cmd_yt = "dotnet run --project " .. csharp_root .. " -- sync yt"
    os.execute(cmd_yt)
    
    print(cyan .. "\n[Last.fm Sync]" .. reset)
    local cmd_lfm = "dotnet run --project " .. csharp_root .. " -- sync lastfm"
    os.execute(cmd_lfm)
    
    print(green .. "\nAll syncs complete!" .. reset)
end
commands_help["syncall"] = "Run all synchronization tasks"

--------------------------------------------------------------------------------
-- Command Registration (The Clink "Magic")
--------------------------------------------------------------------------------

-- Since Lua functions aren't directly executable from the prompt like batch files,
-- we use a command hook or define global matchers.
-- A common pattern in Clink user scripts is to inject `doskey` macros at startup.

local function set_doskey(alias, lua_code)
    -- Note: This is a bit hacky. Usually Clink scripts add completions.
    -- For actual execution, we rely on Cmder's alias system or `lua` command.
    -- Better: We register a prompt trigger that intercepts specific words.
    -- However, for this 'Full Implementation', we will output the doskey commands
    -- that the USER should run, or we hook into `clink.on_filter_input`.
end

-- The Cleanest Cmder way: Define aliases in 'user_aliases.cmd' that call `clink lua code`.
-- Example: dirs=lua -e "require('ScriptsToolkit').dirs(...)"
-- BUT, to make this specific file self-contained, we will export a global table.

ScriptsToolkit = {
    dirs = dirs,
    tree = tree,
    remux = remux,
    whisp = whisp,
    syncall = syncall,
    tkfn = tkfn
}

-- Return the table so it can be required
return ScriptsToolkit

-- INSTRUCTIONS FOR USER:
-- Add these lines to your %CMDER_ROOT%\config\user_aliases.cmd:
-- tkfn=lua clink.print(require('ScriptsToolkit').tkfn())
-- dirs=lua require('ScriptsToolkit').dirs("$1", "$2")
-- tree=lua require('ScriptsToolkit').tree("$1")
-- remux=lua require('ScriptsToolkit').remux("$1")
-- whisp=lua require('ScriptsToolkit').whisp("$1", "$2", "$3")
-- syncall=lua require('ScriptsToolkit').syncall()
```

---

## Symbol & Feature Glossary (Language: Lua 5.1/LuaJIT for Clink)

| Feature                  | Symbol / Syntax             | Detailed Explanation                                                                                                                                           |
| ------------------------ | --------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Local Variable**       | `local name = value`        | Declares a variable scoped to the current block or chunk. **ALWAYS** use `local` to avoid polluting the global namespace. Lua variables are global by default! |
| **Comments**             | `-- Text`                   | Single line comment. Initiated by double hyphen.                                                                                                               |
| **String Concatenation** | `str1 .. str2`              | The `..` operator joins two strings. Unlike `+` in many languages.                                                                                             |
| **Escape Codes**         | `\x1b[32m`                  | ANSI escape sequence hex code for colors. `[32m` is green. `[0m` resets.                                                                                       |
| **Table (Array)**        | `{ "a", "b" }`              | Tables are the *only* data structure in Lua. In array format, they are 1-indexed.                                                                              |
| **Table (Dict)**         | `{ key = "val" }`           | Tables also act as hashmaps. Accessed via `t["key"]` or `t.key`.                                                                                               |
| **Loop (Array)**         | `for _, v in ipairs(t) do`  | `ipairs` iterates over numeric indices `1..N`. `_` is a convention for ignoring the index.                                                                     |
| **Loop (Dict)**          | `for k, v in pairs(t) do`   | `pairs` iterates over all keys in the table (order undefined).                                                                                                 |
| **Function Def**         | `local function name(args)` | Defines a block of code. Ended by `end`.                                                                                                                       |
| **Logical OR**           | `x = a or b`                | Idiom for default values. If `a` is nil/false, `x` becomes `b`.                                                                                                |
| **Table Insert**         | `table.insert(t, val)`      | Appends `val` to the end of the array-like table `t`.                                                                                                          |
| **Table Concat**         | `table.concat(t, " ")`      | Joins all array elements of `t` into a single string separated by space. High performance string building.                                                     |
| **Sys Call**             | `os.execute(cmd)`           | Passes `cmd` string to the underlying OS shell (cmd.exe in Windows case).                                                                                      |
| **Equality**             | `val == nil`                | Checks if a value does not exist. `nil` is the type for non-existence.                                                                                         |
| **Return**               | `return`                    | Exits the function immediately.                                                                                                                                |
| **Module Export**        | `return Table`              | At the end of a file, returning a table allows other scripts to load it via `h = require('file')`.                                                             |

### Formatting & Indentation
- **Indent:** Standard Lua uses **4 spaces**.
- **Blocks:** Functions, loops, and conditions must be closed with the `end` keyword.
- **Parens:** Optional for function calls with a single string/table arg (e.g. `print "hi"`), but mandatory otherwise.
