# Description

-----------------------------

# OCI SSH Master Guide (Consolidated)

## Scope

This is the authoritative consolidated guide for ALL OCI SSH configurations. It replaces all previously fragmented OCI
SSH guides.

## 1. Access Architecture

### 1.1 Access Layers (Priority Order)

| Priority | Layer                              | Role                                        | Fallback                                   |
| -------- | ---------------------------------- | ------------------------------------------- | ------------------------------------------ |
| **1**    | Tailscale SSH                      | Preferred day-to-day access                 | MagicDNS resolves hostname -> Tailscale IP |
| **2**    | OCI Bastion                        | Time-limited private SSH to private targets | No public SSH needed                       |
| **3**    | Direct SSH (key-auth)              | Direct `ssh oci` from Windows               | Requires public IP open                    |
| **4**    | OCI Instance Console (Cloud Shell) | Break-glass emergency recovery              | Creates temporary key for console          |
| **5**    | OCI Instance Console (Local RSA)   | Last-resort recovery                        | Requires local RSA key                     |

### 1.2 Instance Configuration Template

```
Instance Name:  <INSTANCE_NAME>
Compartment:    <COMPARTMENT_NAME> (root)
Region:         <REGION>
Image:          Canonical Ubuntu 24.04 (aarch64)
Shape:          VM.Standard.A1.Flex (4 OCPU, 24 GB RAM)
User:           ubuntu
Data Volume:    <DATA_VOLUME_NAME> (150 GB block volume)
```

## 2. SSH Key Management

### 2.1 Key Paths on Windows

| File                | Path                     | Purpose                         |
| ------------------- | ------------------------ | ------------------------------- |
| Private key         | `~/.ssh/oci`             | OCI outbound SSH                |
| Public key          | `~/.ssh/oci.pub`         | Uploaded to OCI authorized_keys |
| SSH config          | `~/.ssh/config`          | Host alias `oci`                |
| Windows private key | `~/.ssh/id_ed25519`      | Inbound Windows SSH             |
| Windows public key  | `~/.ssh/id_ed25519.pub`  | Added to remote authorized_keys |
| authorized_keys     | `~/.ssh/authorized_keys` | Windows inbound SSH auth        |

### 2.2 Generate New Key Pair

```powershell
ssh-keygen -t ed25519 -f $HOME\.ssh\oci -N "" -C "oci-key-$(Get-Date -Format yyyyMMdd)"
cat $HOME\.ssh\oci.pub
```

### 2.3 Key Locking (Security Hardening)

```powershell
# Lock all .ssh files (no-access ACL)
Get-ChildItem $HOME\.ssh -File | ForEach-Object {
    $acl = New-Object System.Security.AccessControl.FileSecurity
    $acl.SetAccessRuleProtection($true, $false)
    Set-Acl -LiteralPath $_.FullName -AclObject $acl
}
```

## 3. Windows -> OCI SSH (Outbound)

### 3.1 SSH Config Block

```
Host oci
    HostName <OCI_PUBLIC_IP>
    User ubuntu
    IdentityFile ~/.ssh/oci
    IdentitiesOnly yes
    ServerAliveInterval 30
    ServerAliveCountMax 3
```

### 3.2 Troubleshooting Outbound

| Issue                         | Resolution                                         |
| ----------------------------- | -------------------------------------------------- |
| Permission denied (publickey) | Verify `.pub` matches key on OCI `authorized_keys` |
| Connection timed out          | Check OCI security list allows inbound TCP 22      |
| Host key verification failed  | `ssh-keyscan -H <IP> >> ~/.ssh/known_hosts`        |

## 4. Inbound Windows SSH (Remote -> Windows)

### 4.1 Install and Configure OpenSSH Server

```powershell
Add-WindowsCapability -Online -Name 'OpenSSH.Server~~~~0.0.1.0'
Set-Service -Name 'sshd' -StartupType Automatic
Start-Service -Name 'sshd'
```

### 4.2 Current Windows SSH State

* `sshd` service: Running, Automatic startup
* Listener: Tailscale IP only
* `PubkeyAuthentication yes`, `PasswordAuthentication no`
* `AuthorizedKeysFile .ssh/authorized_keys`

## 5. Tailscale-Only SSH Configuration

### 5.1 Configuration

```ssh
ListenAddress <TAILSCALE_IP>
PubkeyAuthentication yes
PasswordAuthentication no
AuthorizedKeysFile .ssh/authorized_keys
```

### 5.2 MagicDNS Setup

Enable MagicDNS in Tailscale Admin Console -> DNS -> MagicDNS.

### 5.3 Hard Lock Prevention

| Scenario                  | Risk                  | Mitigation                                      |
| ------------------------- | --------------------- | ----------------------------------------------- |
| Tailscale service crashes | Cannot SSH in         | Keep local console access; enable RDP as backup |
| Network partition         | Tailscale unreachable | Use Emergency Recovery Procedure                |

## 6. OCI SSH Key Reset (Break-Glass Recovery)

### Preferred: Cloud Shell Serial Console

1. OCI Console -> Instance -> Console connection
2. Launch Cloud Shell connection
3. Reach root shell, fix authorized_keys, reboot

### Key Replacement on Ubuntu 24.04

```bash
mount -o remount,rw /
cd /home/ubuntu/.ssh
mv authorized_keys authorized_keys.old
echo "ssh-ed25519 AAAAC3..." > authorized_keys
chmod 700 /home/ubuntu/.ssh
chmod 600 /home/ubuntu/.ssh/authorized_keys
reboot -f
```

## 7. Data Preservation & Recovery

* **Block Volume**: Data volume (150 GB) - Preserve app data across rebuilds
* **Nuclear Recovery**: Terminate -> Create -> Reattach -> Harden -> Rebuild -> Verify -> Update scripts
* **Instance Parameters**: 4 OCPU, 24 GB, Ubuntu 24.04 ARM, existing VCN

## 8. Media Stack Monitoring

| Tool            | Role                                   |
| --------------- | -------------------------------------- |
| **Uptime Kuma** | Primary health monitor and alerting    |
| **Dozzle**      | Live container log viewer              |
| **Emby**        | Media server (loopback 127.0.0.1:8096) |

### VPN Isolation (Gluetun)

* Use `network_mode: service:gluetun` ONLY for containers that must be forced through VPN
* Keep unrelated services on normal bridge network

## 9. Verification Checklist

### Windows -> OCI

- [ ] Private key exists at `~/.ssh/oci`
- [ ] SSH config has `Host oci` block
- [ ] TCP port 22 reachable
- [ ] SSH handshake succeeds

### Inbound Windows SSH

- [ ] `sshd` service running
- [ ] Listening ONLY on Tailscale IP
- [ ] NOT listening on public IP

### Emergency Recovery

- [ ] Serial console access verified
- [ ] `authorized_keys` replacement procedure documented
- [ ] Block volume reattachment documented

## 10. Troubleshooting Matrix

| Symptom                         | Check              | Fix                                              |
| ------------------------------- | ------------------ | ------------------------------------------------ |
| "Permission denied (publickey)" | Key mismatch       | Verify `.pub` matches OCI `authorized_keys`      |
| "Connection timed out"          | Port closed        | Check OCI security list / Windows Firewall       |
| "Connection refused"            | sshd not listening | `Get-Service sshd`, check `sshd_config`          |
| Can't connect via Tailscale     | Tailscale down     | Check `tailscale status`, use recovery procedure |
| Host key changed                | Instance rebuilt   | `ssh-keygen -R <IP>`                             |

## 11. Deprecated Guides

All previous OCI SSH guides are superseded by this Master Guide. Their content has been merged into appropriate sections
above.
