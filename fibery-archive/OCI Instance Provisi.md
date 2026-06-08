# Description

-----------------------------

# OCI Instance Provisioning

## Infrastructure Profile

* **Shape:** VM.Standard.A1.Flex (4 OCPU, 24GB RAM)
* **Region:** ap-hyderabad-1
* **Image:** Canonical Ubuntu 24.04 aarch64
* **Boot Volume:** 47GB

## Create Instance

1. Navigate to OCI Console → Compute → Instances → Create instance
2. Configure:
	* **Name:** `media-server`
	* **Compartment:** root
	* **Placement:** ap-hyderabad-1, AD-1
	* **Image:** Canonical Ubuntu 24.04 (aarch64)
	* **Shape:** VM.Standard.A1.Flex → 4 OCPUs, 24 GB memory
	* **Boot Volume:** Custom size → 47 GB
	* **Networking:** Existing VCN `media-server-vcn`, public subnet, assign public IPv4
	* **SSH Key:** Paste public key (ed25519)
3. Wait for instance to reach Running state
4. SSH into instance: `ssh -i ~/.ssh/oci_id_ed25519 ubuntu@<public-ip>`
