# Description

-----------------------------

# Bitwarden Windows SSH Setup

**Bitwarden CLI vs. Desktop:**\
The `bw` CLI tool *does not* natively act as an SSH agent. However, the Bitwarden Desktop app *does* feature an
integrated SSH agent (`bwarden-ssh-agent`) that works natively on Windows.

To pass your Bitwarden keys via SSH inside Windows Terminal, you should:

1. Open Bitwarden Desktop Settings and check **Enable SSH Agent**.
2. Open Windows Services (`services.msc`) and **Disable** the `OpenSSH Authentication Agent` service (so Bitwarden can
   take over the named pipe).
3. From Windows Terminal, standard `ssh` commands will automatically prompt Bitwarden for keys.
