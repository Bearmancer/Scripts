# Description

-----------------------------

# Data Volume Recovery & Mounting

## 1. TERMINATE OLD (Recovery)

* **Action**: In OCI Console, navigate to Compute -> Instances. Select the compromised/failing instance.
* **Action**: Click "Terminate". **CRITICAL**: Ensure "Permanently delete the attached Boot Volume" is CHECKED, but "
  Permanently delete attached Block Volumes" is UNCHECKED.
* **Verification**: Go to Storage -> Block Volumes. The 150GB volume state must be `AVAILABLE`.

## 2. REATTACH DATA VOLUME

* **Action**: OCI Console -> Storage -> Block Volumes -> Select 150GB volume -> Attach to Instance (Paravirtualized,
  Read/Write).
* **Action**: SSH into instance. Run `lsblk` to identify the drive (e.g., `sdb`).
* **Action**: `sudo mkdir -p /data && sudo mount /dev/sdb /data`.
* **Action**: Get UUID with `blkid /dev/sdb`. Add to fstab: `UUID=<UUID> /data ext4 defaults,_netdev 0 2`.
* **Verification**: `ls -l /data` displays existing media directories.
