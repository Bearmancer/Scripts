# Description

-----------------------------

# Ubuntu Host Hardening

## Hardening Steps

```bash
# Set password for ubuntu user
sudo passwd ubuntu

# Enable password SSH as backup
sudo sed -i 's/PasswordAuthentication no/PasswordAuthentication yes/' /etc/ssh/sshd_config.d/60-cloudimg-settings.conf
sudo systemctl restart sshd

# Fix GRUB timeout for recovery access
echo 'GRUB_TIMEOUT=10' | sudo tee /etc/default/grub.d/99-recovery.cfg
echo 'GRUB_TIMEOUT_STYLE=menu' | sudo tee -a /etc/default/grub.d/99-recovery.cfg
sudo update-grub

# Backup iptables rules
sudo iptables-save | sudo tee /etc/iptables/rules.v4.bak

# Install Tailscale with SSH
curl -fsSL https://tailscale.com/install.sh | sh
sudo tailscale up --ssh
```

## Prevention Measures

* Set password for `ubuntu` user at setup
* Keep GRUB timeout > 0
* Backup SSH keys
* Configure OCI Bastion Service