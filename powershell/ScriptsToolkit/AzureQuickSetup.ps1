<#
.SYNOPSIS
    End-to-end Azure account setup from scratch - minimal complexity path.

.DESCRIPTION
    Automates Azure subscription setup after creating an account.
    Uses az CLI exclusively for all operations.
    Handles missing DAC account gracefully with interactive prompts.

.USAGE
    .\AzureQuickSetup.ps1 [-SubscriptionName "my-sub"] [-Region "eastus"]

.NOTES
    Requires: Azure CLI installed, internet connection
#>

param(
    [string]$SubscriptionName,
    [string]$Region = "eastus",
    [string]$ResourceGroup,
    [string[]]$Tags = @()
)

$ErrorActionPreference = "Stop"

# ============================================
# STEP 1: Verify Azure CLI Installation
# ============================================
Write-Host "=== Azure CLI Verification ===" -ForegroundColor Cyan

$azExists = Get-Command az -ErrorAction SilentlyContinue
if (-not $azExists) {
    throw "Azure CLI not found. Install from: https://aka.ms/installazurecli"
}

$azVersion = az version --output json 2>&1 | ConvertFrom-Json
Write-Host "Azure CLI version: $($azVersion.'azure-cli')" -ForegroundColor Green

# ============================================
# STEP 2: Check Account Login Status
# ============================================
Write-Host "`n=== Account Login Status ===" -ForegroundColor Cyan

$account = az account show --output json 2>&1 | ConvertFrom-Json
if ($LASTEXITCODE -ne 0 -or -not $account) {
    Write-Host "Not logged in. Initiating login..." -ForegroundColor Yellow
    
    # Check if azd is available for better UX
    $azdAvailable = Get-Command azd -ErrorAction SilentlyContinue
    if ($azdAvailable) {
        Write-Host "Using Azure Developer CLI for login..." -ForegroundColor Cyan
        azd auth login --scope organizations
    } else {
        Write-Host "Using Azure CLI login (browser will open)..." -ForegroundColor Cyan
        az login
    }
    
    $account = az account show --output json 2>&1 | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or -not $account) {
        throw "Login failed. Please try again."
    }
}

Write-Host "✓ Logged in as: $($account.user.name)" -ForegroundColor Green
Write-Host "✓ Tenant: $($account.tenantId)" -ForegroundColor Green

# ============================================
# STEP 3: Handle Subscription Selection
# ============================================
Write-Host "`n=== Subscription Setup ===" -ForegroundColor Cyan

$subs = az account list --output json 2>&1 | ConvertFrom-Json

# If no subscriptions, need to create one via portal
if ($subs.Count -eq 0) {
    Write-Host "No subscriptions found." -ForegroundColor Yellow
    Write-Host "New Azure accounts need subscription created in Azure portal first:" -ForegroundColor Cyan
    Write-Host "  https://portal.azure.com/#blade/Microsoft_Azure_Billing/SubscriptionsBlade" -ForegroundColor White
    Write-Host ""
    $createNow = Read-Host "Open portal now to create subscription? (y/n)"
    if ($createNow -eq 'y') {
        Start-Process "https://portal.azure.com/#blade/Microsoft_Azure_Billing/SubscriptionsBlade"
    }
    throw "Subscription required. Create one in portal, then re-run this script."
}

# Set or select subscription
if (-not $SubscriptionName) {
    Write-Host "Available subscriptions:" -ForegroundColor Cyan
    $subs | Format-Table name, id, state -AutoSize
    $SubscriptionName = Read-Host "Enter subscription name to use"
}

$targetSub = $subs | Where-Object { $_.name -eq $SubscriptionName }
if (-not $targetSub) {
    $targetSub = $subs | Where-Object { $_.id -eq $SubscriptionName }
}

if (-not $targetSub) {
    Write-Host "Subscription '$SubscriptionName' not found." -ForegroundColor Red
    Write-Host "Available:" -ForegroundColor Yellow
    $subs | ForEach-Object { Write-Host "  - $($_.name) ($($_.id))" }
    throw "Invalid subscription name."
}

# Set active subscription
az account set --subscription $targetSub.id
Write-Host "✓ Active subscription: $($targetSub.name)" -ForegroundColor Green

# ============================================
# STEP 4: Set Default Region
# ============================================
Write-Host "`n=== Region Configuration ===" -ForegroundColor Cyan

if (-not $Region) {
    $Region = Read-Host "Enter preferred region (e.g., eastus, westeurope)"
}

# Verify region is valid
$validRegions = az account list-locations --output json | ConvertFrom-Json | 
    Where-Object { $_.metadata.regionType -ne "Pseudo" } | 
    Select-Object -ExpandProperty name

if ($validRegions -notcontains $Region) {
    Write-Host "Warning: $Region may not be valid. Common regions: eastus, westus2, westeurope, southeastasia" -ForegroundColor Yellow
}

az configure --defaults location=$Region
Write-Host "✓ Default region set to: $Region" -ForegroundColor Green

# ============================================
# STEP 5: Create Resource Group
# ============================================
Write-Host "`n=== Resource Group Creation ===" -ForegroundColor Cyan

if (-not $ResourceGroup) {
    $ResourceGroup = Read-Host "Enter resource group name (e.g., rg-main)"
}

$existingRg = az group show --name $ResourceGroup --output json 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "Creating resource group: $ResourceGroup" -ForegroundColor Cyan
    
    if ($Tags.Count -gt 0) {
        az group create --name $ResourceGroup --location $Region --tags ($Tags -join " ")
    } else {
        az group create --name $ResourceGroup --location $Region
    }
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ Resource group created: $ResourceGroup" -ForegroundColor Green
    } else {
        throw "Failed to create resource group"
    }
} else {
    Write-Host "✓ Resource group already exists: $ResourceGroup" -ForegroundColor Green
}

# ============================================
# STEP 6: Enable Required Providers
# ============================================
Write-Host "`n=== Enabling Resource Providers ===" -ForegroundColor Cyan

$providers = @(
    "Microsoft.Compute",
    "Microsoft.Storage", 
    "Microsoft.Network",
    "Microsoft.KeyVault",
    "Microsoft.App",
    "Microsoft.OperationalInsights",
    "Microsoft.Insights"
)

foreach ($provider in $providers) {
    Write-Host "Registering $provider..." -ForegroundColor Gray
    az provider register --namespace $provider --wait
}

Write-Host "✓ All providers registered" -ForegroundColor Green

# ============================================
# STEP 7: Create Storage Account (Optional but Recommended)
# ============================================
Write-Host "`n=== Creating Storage Account (Optional) ===" -ForegroundColor Cyan

$createStorage = Read-Host "Create a storage account for scripts? (y/n)"
if ($createStorage -eq 'y') {
    $storageName = Read-Host "Enter storage account name (3-24 chars, lowercase)"
    if ([string]::IsNullOrWhiteSpace($storageName)) {
        $storageName = "st$(Get-Random -Minimum 1000 -Maximum 9999)"
    }
    
    az storage account create `
        --name $storageName `
        --resource-group $ResourceGroup `
        --location $Region `
        --sku Standard_LRS `
        --kind StorageV2 `
        --output none
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ Storage account created: $storageName" -ForegroundColor Green
    }
}

# ============================================
# STEP 8: Summary and Next Steps
# ============================================
Write-Host "`n" + "="*50 -ForegroundColor Green
Write-Host "AZURE SETUP COMPLETE" -ForegroundColor Green
Write-Host "="*50 -ForegroundColor Green

Write-Host @"

Configuration Summary:
  Subscription: $($targetSub.name)
  Tenant ID:    $($account.tenantId)
  Region:       $Region
  Resource Group: $ResourceGroup

Next Steps:
  1. Run 'az login' on new terminals
  2. Deploy resources: az deployment group create
  3. View resources: az resource list --resource-group $ResourceGroup

Quick Reference:
  Set subscription: az account set --subscription '$($targetSub.name)'
  Set region:       az configure --defaults location=$Region
  Create resource:  az group deployment create

"@ -ForegroundColor Cyan