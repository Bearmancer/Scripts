# C:\Users\Lance\Dev\Scripts\powershell\ScriptsToolkit\deploy_oci_postgres.ps1
$ErrorActionPreference = 'Stop'

# Create remote directory
ssh oci "mkdir -p /home/ubuntu/postgres"

# Write docker-compose.yml remotely
$composeContent = @"
services:
  postgres:
    image: postgres:18
    container_name: postgres
    environment:
      POSTGRES_DB: pg_db
      POSTGRES_USER: lance
      POSTGRES_PASSWORD: lance
      PGDATA: /var/lib/postgresql/data/pgdata
    ports:
      - `"5432:5432`"
    volumes:
      - postgres_data:/var/lib/postgresql/data
    restart: unless-stopped
volumes:
  postgres_data:
    driver: local
"@

$tempFile = [System.IO.Path]::GetTempFileName()
$composeContent | Out-File -FilePath $tempFile -Encoding utf8
scp $tempFile oci:/home/ubuntu/postgres/docker-compose.yml
Remove-Item $tempFile

# Run container stack
ssh oci "cd /home/ubuntu/postgres && docker compose up -d"
Write-Host "OCI PostgreSQL stack deployed successfully."
