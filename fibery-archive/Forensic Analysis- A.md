# Description

-----------------------------

# Forensic Analysis: Architectural Shift from .copilot to Fibery

Historically, `.copilot` was targeted and required as the source of truth because Copilot was inherently coupled to
Cline. Artifacts such as skills, prompts, and global instructions were sprawled across local directories (`~/.copilot`,
`~/.config/kilo`, `~/Documents/Cline/Rules`), creating a highly fragile execution environment.

This setup caused chronic failures because subagents lacked a centralized, immutable synchronization point. A single
mismatch between `.clinerules` in a project and a skill definition in `.copilot` could trigger infinite retry loops.

Moving the canonical state to Fibery `Skills/Skill`, `Knowledge/Guide`, and `Repos/Issue` entirely eliminates local
duplication. Agents now dynamically fetch real-time state using API queries, ensuring perfect cohesion and dramatically
reducing the static token payload.
