$ErrorActionPreference = 'Stop'

$gitignore = ".gitignore"
if (Test-Path $gitignore) {
	$content = Get-Content $gitignore -Raw
	if ($content -notmatch '(?m)^\.env$') {
		Add-Content $gitignore "`n.env"
		Write-Host "Added .env to .gitignore"
	}
}
else {
	Set-Content $gitignore ".env"
	Write-Host "Created .gitignore and added .env"
}

$iteration = 1
while ($true) {
	Write-Host "--- Iteration $iteration ---"

	if (Test-Path local_leaks.json) {
		Remove-Item local_leaks.json
	}

	$proc = Start-Process gitleaks -ArgumentList "detect --no-git --report-path local_leaks.json -f json" -NoNewWindow -Wait -PassThru

	if ($proc.ExitCode -eq 0 -or !(Test-Path local_leaks.json)) {
		Write-Host "Success! No local leaks found in the working directory."
		break
	}

	$leaks = Get-Content local_leaks.json -Raw | ConvertFrom-Json
	if (-not $leaks) {
		Write-Host "Success! No local leaks found in the working directory."
		break
	}

	Write-Host "Found $( $leaks.Count ) leaks."

	$modifiedFiles = @{ }

	foreach ($leak in $leaks) {
		$file = $leak.File
		$secret = $leak.Secret
		$rule = $leak.RuleID

		if (-not (Test-Path $file)) {
			continue
		}

		$envExists = Test-Path .env
		$envContent = if ($envExists) {
			Get-Content .env -Raw
		}
		else {
			""
		}

		if ($envContent -notmatch [regex]::Escape($secret)) {
			$envKey = "RECOVERED_$( $rule.ToUpper() -replace '-', '_' )_$( Get-Random -Maximum 9999 )"
			Add-Content .env "$envKey=$secret"
			Write-Host "Saved secret to .env as $envKey"
		}

		if (-not $modifiedFiles.ContainsKey($file)) {
			$modifiedFiles[$file] = Get-Content $file -Raw
		}

		$modifiedFiles[$file] = $modifiedFiles[$file].Replace($secret, "[REDACTED_$($rule.ToUpper() )]")
	}

	foreach ($file in $modifiedFiles.Keys) {
		[IO.File]::WriteAllText((Resolve-Path $file).Path, $modifiedFiles[$file])
		Write-Host "Redacted secrets in $file"
	}

	$iteration++
	if ($iteration -gt 10) {
		Write-Host "Hit iteration limit of 10. Aborting to prevent infinite loop."
		break
	}
}
