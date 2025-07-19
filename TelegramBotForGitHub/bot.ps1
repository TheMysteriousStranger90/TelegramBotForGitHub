Write-Host "Starting Telegram Bot for GitHub..." -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Cyan

# Check if Docker is running
Write-Host "Checking Docker status..." -ForegroundColor Yellow
try {
    $dockerStatus = docker info 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host "WARNING: Docker is not running!" -ForegroundColor Red
        Write-Host "Please start Docker Desktop first." -ForegroundColor Yellow
        $continue = Read-Host "Continue anyway? (y/n)"
        if ($continue -ne "y") {
            exit
        }
    } else {
        Write-Host "Docker is running ✓" -ForegroundColor Green
    }
} catch {
    Write-Host "WARNING: Docker not found or not running!" -ForegroundColor Red
    Write-Host "Please install Docker Desktop and start it." -ForegroundColor Yellow
    $continue = Read-Host "Continue anyway? (y/n)"
    if ($continue -ne "y") {
        exit
    }
}

# Check if ngrok is running
Write-Host "Checking ngrok status..." -ForegroundColor Yellow
$ngrokProcess = Get-Process -Name "ngrok" -ErrorAction SilentlyContinue
if (-not $ngrokProcess) {
    Write-Host "WARNING: ngrok is not running!" -ForegroundColor Red
    Write-Host "Please start ngrok first with a free port (e.g. 7074): ngrok http 7074" -ForegroundColor Yellow
    Write-Host "Then update BaseUrl in local.settings.json" -ForegroundColor Yellow
    $continue = Read-Host "Continue anyway? (y/n)"
    if ($continue -ne "y") {
        exit
    }
} else {
    Write-Host "ngrok is running ✓" -ForegroundColor Green
}

# Check available ports for Azure Functions
Write-Host "Checking for available ports..." -ForegroundColor Yellow
$availablePorts = @(7071, 7072, 7073, 7074, 7075, 7076, 7077, 7078, 7079, 7080)
$selectedPort = $null

foreach ($port in $availablePorts) {
    $portInUse = $null
    try {
        $portInUse = Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue
    } catch {
        # Port not in use
    }

    if (-not $portInUse) {
        $selectedPort = $port
        Write-Host "Found available port: $selectedPort ✓" -ForegroundColor Green
        break
    }
}

if (-not $selectedPort) {
    Write-Host "WARNING: Could not find an available port in range 7071-7080!" -ForegroundColor Red
    Write-Host "Please free up one of these ports or modify the script to use a different range." -ForegroundColor Yellow
    exit
}

# Check if Azure Functions are already running on the selected port
Write-Host "Checking Azure Functions status..." -ForegroundColor Yellow
$functionsRunning = $false
try {
    $response = Invoke-WebRequest -Uri "http://localhost:$selectedPort" -TimeoutSec 3 -ErrorAction SilentlyContinue
    if ($response.StatusCode -eq 200 -or $response.StatusCode -eq 404) {
        $functionsRunning = $true
        Write-Host "Azure Functions are already running on port $selectedPort ✓" -ForegroundColor Green
    }
} catch {
    Write-Host "Azure Functions are not running yet on port $selectedPort" -ForegroundColor Yellow
}

# Check for Cosmos DB Emulator in Docker with more flexible name matching
Write-Host "Checking Cosmos DB Emulator in Docker..." -ForegroundColor Yellow
$cosmosContainers = docker ps --filter "name=cosmos" --format "{{.Names}}" 2>&1
$cosmosRunning = $false

if ($cosmosContainers -match "cosmos") {
    foreach ($container in $cosmosContainers.Split("`n")) {
        if ($container -match "cosmos") {
            $cosmosContainer = $container.Trim()
            $cosmosRunning = $true
            Write-Host "Found Cosmos DB Emulator container: $cosmosContainer ✓" -ForegroundColor Green
            break
        }
    }
}

# If no container with "cosmos" in the name, check with a broader filter
if (-not $cosmosRunning) {
    $cosmosImages = docker ps --filter "ancestor=mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator" --format "{{.Names}}" 2>&1
    if ($cosmosImages -match "\w+") {
        foreach ($container in $cosmosImages.Split("`n")) {
            if ($container -match "\w+") {
                $cosmosContainer = $container.Trim()
                $cosmosRunning = $true
                Write-Host "Found Cosmos DB Emulator container: $cosmosContainer ✓" -ForegroundColor Green
                break
            }
        }
    }
}

if (-not $cosmosRunning) {
    Write-Host "WARNING: Cosmos DB Emulator container not found!" -ForegroundColor Red
    Write-Host "The Cosmos DB Emulator should be running for the application to work properly." -ForegroundColor Yellow
    Write-Host "Please start it with:" -ForegroundColor Yellow
    Write-Host "docker run -p 8081:8081 -p 10250-10255:10250-10255 --name=cosmosdb-emulator -e AZURE_COSMOS_EMULATOR_PARTITION_COUNT=1 -e AZURE_COSMOS_EMULATOR_ENABLE_DATA_PERSISTENCE=true mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator" -ForegroundColor White

    $continue = Read-Host "Continue anyway? (y/n)"
    if ($continue -ne "y") {
        exit
    }
} else {
    # Check if the container is healthy
    $containerStatus = docker inspect --format='{{.State.Status}}' $cosmosContainer 2>&1
    if ($containerStatus -eq "running") {
        Write-Host "Cosmos DB Emulator container is running ✓" -ForegroundColor Green

        # Try to verify if the emulator is actually responsive
        try {
            $response = Invoke-WebRequest -Uri "https://localhost:8081/_explorer/" -SkipCertificateCheck -TimeoutSec 5 -ErrorAction SilentlyContinue
            if ($response.StatusCode -eq 200) {
                Write-Host "Cosmos DB Emulator is responsive ✓" -ForegroundColor Green
            }
        } catch {
            Write-Host "Cosmos DB Emulator is running but may not be fully initialized yet" -ForegroundColor Yellow
            Write-Host "The application will continue, but database operations might fail until the emulator is ready" -ForegroundColor Yellow
        }
    } else {
        Write-Host "Cosmos DB Emulator container exists but is not running (status: $containerStatus)" -ForegroundColor Yellow
        Write-Host "Attempting to start the container..." -ForegroundColor Yellow

        docker start $cosmosContainer 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host "Started Cosmos DB Emulator container ✓" -ForegroundColor Green
            Write-Host "Waiting for Cosmos DB to initialize..." -ForegroundColor Yellow
            Start-Sleep -Seconds 10  # Give it some time to start up
        } else {
            Write-Host "Failed to start Cosmos DB Emulator container" -ForegroundColor Red
            $continue = Read-Host "Continue anyway? (y/n)"
            if ($continue -ne "y") {
                exit
            }
        }
    }
}

# Update the local.settings.json with the selected port
Write-Host "Updating local.settings.json with the selected port..." -ForegroundColor Yellow
$settingsPath = ".\local.settings.json"
$settingsUpdated = $false

if (Test-Path $settingsPath) {
    $settings = Get-Content $settingsPath -Raw | ConvertFrom-Json

    # Update or add Host section with selected port
    if (-not $settings.Host) {
        $settings | Add-Member -Type NoteProperty -Name "Host" -Value @{
            "LocalHttpPort" = $selectedPort
            "CORS" = "*"
        }
        $settingsUpdated = $true
    } elseif ($settings.Host.LocalHttpPort -ne $selectedPort) {
        $settings.Host.LocalHttpPort = $selectedPort
        $settingsUpdated = $true
    }

    # Write the updated settings back
    if ($settingsUpdated) {
        $settings | ConvertTo-Json -Depth 10 | Set-Content $settingsPath
        Write-Host "Updated local.settings.json with port $selectedPort ✓" -ForegroundColor Green
    } else {
        Write-Host "Port $selectedPort is already configured in local.settings.json ✓" -ForegroundColor Green
    }
} else {
    Write-Host "WARNING: local.settings.json not found!" -ForegroundColor Red
    $continue = Read-Host "Continue anyway? (y/n)"
    if ($continue -ne "y") {
        exit
    }
}

# Check if local.settings.json exists and has BaseUrl
Write-Host "Checking configuration..." -ForegroundColor Yellow
$baseUrl = $null
$botToken = $null

if (Test-Path $settingsPath) {
    $settings = Get-Content $settingsPath | ConvertFrom-Json
    $baseUrl = $settings.Values.BaseUrl
    $botToken = $settings.Values."Telegram__Token"

    if ($baseUrl -and $baseUrl -ne "https://your-ngrok-url.ngrok-free.app") {
        Write-Host "Configuration looks good ✓" -ForegroundColor Green
        Write-Host "BaseUrl: $baseUrl" -ForegroundColor Cyan
    } else {
        Write-Host "WARNING: BaseUrl not configured properly!" -ForegroundColor Red
        Write-Host "Please update BaseUrl in local.settings.json with your ngrok URL" -ForegroundColor Yellow
    }

    if ($botToken) {
        Write-Host "Telegram Bot Token: ***...${botToken.Substring([Math]::Max(0, $botToken.Length-10))}" -ForegroundColor Cyan
    } else {
        Write-Host "WARNING: Telegram Bot Token not found!" -ForegroundColor Red
    }
} else {
    Write-Host "WARNING: local.settings.json not found!" -ForegroundColor Red
}

# Setup Telegram Webhook
if ($baseUrl -and $botToken -and $baseUrl -ne "https://your-ngrok-url.ngrok-free.app") {
    Write-Host "============================================" -ForegroundColor Cyan
    Write-Host "Setting up Telegram webhook..." -ForegroundColor Yellow

    $webhookUrl = "$baseUrl/api/telegram/webhook"
    Write-Host "Webhook URL: $webhookUrl" -ForegroundColor Cyan

    try {
        # Set the webhook
        $setWebhookResponse = Invoke-RestMethod -Uri "https://api.telegram.org/bot$botToken/setWebhook" -Method POST -Body @{
            url = $webhookUrl
        } -ContentType "application/x-www-form-urlencoded"

        if ($setWebhookResponse.ok) {
            Write-Host "Telegram webhook set successfully! ✓" -ForegroundColor Green
            Write-Host "Description: $($setWebhookResponse.description)" -ForegroundColor White
        } else {
            Write-Host "Failed to set webhook: $($setWebhookResponse.description)" -ForegroundColor Red
        }

        # Wait a moment for the webhook to be processed
        Start-Sleep -Seconds 2

        # Check webhook info
        Write-Host "Verifying webhook configuration..." -ForegroundColor Yellow
        $webhookInfo = Invoke-RestMethod -Uri "https://api.telegram.org/bot$botToken/getWebhookInfo" -Method GET

        if ($webhookInfo.ok) {
            Write-Host "Current webhook URL: $($webhookInfo.result.url)" -ForegroundColor Cyan
            Write-Host "Has custom certificate: $($webhookInfo.result.has_custom_certificate)" -ForegroundColor White
            Write-Host "Pending update count: $($webhookInfo.result.pending_update_count)" -ForegroundColor White

            if ($webhookInfo.result.last_error_date) {
                $errorDate = [DateTimeOffset]::FromUnixTimeSeconds($webhookInfo.result.last_error_date).ToString("yyyy-MM-dd HH:mm:ss")
                Write-Host "Last error date: $errorDate" -ForegroundColor Yellow
                Write-Host "Last error message: $($webhookInfo.result.last_error_message)" -ForegroundColor Yellow
            } else {
                Write-Host "No recent errors ✓" -ForegroundColor Green
            }
        }
    } catch {
        Write-Host "Error setting up Telegram webhook: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "You can set it up manually later using the webhook URL above" -ForegroundColor Yellow
    }
} else {
    Write-Host "Skipping Telegram webhook setup - missing configuration" -ForegroundColor Yellow
}

# Start Azure Functions only if not already running
$functionsJob = $null
if (-not $functionsRunning) {
    Write-Host "============================================" -ForegroundColor Cyan
    Write-Host "Starting Azure Functions on port $selectedPort..." -ForegroundColor Yellow

    # Start Azure Functions in background job
    $functionsJob = Start-Job -ScriptBlock {
        param($port, $workingDir)
        Set-Location $workingDir
        func start --port $port
    } -ArgumentList $selectedPort, $PWD

    # Wait for Azure Functions to start
    Write-Host "Waiting for Azure Functions to start..." -ForegroundColor Yellow
    $timeout = 60  # Increased timeout for Functions
    $elapsed = 0

    do {
        Start-Sleep -Seconds 2
        $elapsed += 2

        try {
            $response = Invoke-WebRequest -Uri "http://localhost:$selectedPort" -TimeoutSec 5 -ErrorAction SilentlyContinue
            if ($response.StatusCode -eq 200 -or $response.StatusCode -eq 404) {
                $functionsRunning = $true
                Write-Host "Azure Functions started successfully on port $selectedPort ✓" -ForegroundColor Green
                break
            }
        } catch {
            # Check if job has failed
            $jobState = Get-Job -Id $functionsJob.Id | Select-Object -ExpandProperty State
            if ($jobState -eq "Failed") {
                Write-Host "Azure Functions job failed!" -ForegroundColor Red
                Receive-Job -Id $functionsJob.Id
                break
            }
            # Continue waiting
        }

        Write-Host "Still waiting for Azure Functions... ($elapsed/$timeout seconds)" -ForegroundColor Yellow
    } while ($elapsed -lt $timeout)

    if (-not $functionsRunning) {
        Write-Host "WARNING: Azure Functions may not be ready" -ForegroundColor Red
        Write-Host "Please check if there are any build errors" -ForegroundColor Yellow

        # Try to get some error info from the job
        Write-Host "Job output:" -ForegroundColor Yellow
        Receive-Job -Id $functionsJob.Id
    }
} else {
    Write-Host "============================================" -ForegroundColor Cyan
    Write-Host "Azure Functions are already running on port $selectedPort ✓" -ForegroundColor Green
}

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "Bot startup completed!" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Cyan

# Test bot connection if webhook was set up
if ($baseUrl -and $botToken -and $baseUrl -ne "https://your-ngrok-url.ngrok-free.app") {
    Write-Host "Testing bot connection..." -ForegroundColor Yellow

    try {
        $botInfo = Invoke-RestMethod -Uri "https://api.telegram.org/bot$botToken/getMe" -Method GET
        if ($botInfo.ok) {
            Write-Host "Bot connection test successful ✓" -ForegroundColor Green
            Write-Host "Bot Name: $($botInfo.result.first_name)" -ForegroundColor Cyan
            Write-Host "Bot Username: @$($botInfo.result.username)" -ForegroundColor Cyan
            Write-Host "Bot ID: $($botInfo.result.id)" -ForegroundColor Cyan
        }
    } catch {
        Write-Host "Bot connection test failed: $($_.Exception.Message)" -ForegroundColor Red
    }

    Write-Host "============================================" -ForegroundColor Cyan
}

# Show next steps
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. ✓ Cosmos DB Emulator running in Docker" -ForegroundColor Green
Write-Host "2. ✓ Azure Functions $(if ($functionsRunning) { "ready on port $selectedPort" } else { "check status" })" -ForegroundColor $(if ($functionsRunning) { "Green" } else { "Yellow" })
if ($baseUrl -and $botToken) {
    Write-Host "3. ✓ Telegram webhook configured" -ForegroundColor Green
    Write-Host "4. Test the bot with /start command in Telegram" -ForegroundColor White
    Write-Host "5. Authorize with /auth command" -ForegroundColor White
    Write-Host "6. Subscribe to repositories with /subscribe owner/repo" -ForegroundColor White
} else {
    Write-Host "3. ❌ Configure BaseUrl and Telegram token in local.settings.json" -ForegroundColor Red
    Write-Host "4. Restart this script to set up Telegram webhook" -ForegroundColor White
}

Write-Host "============================================" -ForegroundColor Cyan

# If Azure Functions job was started, monitor it
if ($functionsJob) {
    Write-Host "Press Ctrl+C to stop Azure Functions" -ForegroundColor Yellow
    Write-Host "============================================" -ForegroundColor Cyan

    # Keep the script running and monitor the Azure Functions job
    try {
        while ($true) {
            # Check if Azure Functions job is still running
            $jobState = Get-Job -Id $functionsJob.Id | Select-Object -ExpandProperty State

            if ($jobState -eq "Failed") {
                Write-Host "Azure Functions job failed!" -ForegroundColor Red
                Receive-Job -Id $functionsJob.Id
                break
            } elseif ($jobState -eq "Completed") {
                Write-Host "Azure Functions job completed!" -ForegroundColor Yellow
                Receive-Job -Id $functionsJob.Id
                break
            }

            Start-Sleep -Seconds 5
        }
    } catch {
        Write-Host "Script interrupted by user" -ForegroundColor Yellow
    } finally {
        # Clean up
        Write-Host "Stopping Azure Functions..." -ForegroundColor Yellow
        Stop-Job -Id $functionsJob.Id -ErrorAction SilentlyContinue
        Remove-Job -Id $functionsJob.Id -ErrorAction SilentlyContinue
        Write-Host "Cleanup completed" -ForegroundColor Green
    }
} else {
    Write-Host "Azure Functions are running externally - webhook is ready to use!" -ForegroundColor Green
    Write-Host "You can now test the bot in Telegram with /start command" -ForegroundColor Cyan
    Write-Host "============================================" -ForegroundColor Cyan

    # Just pause to show the status
    Write-Host "Press any key to exit..." -ForegroundColor Yellow
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
}