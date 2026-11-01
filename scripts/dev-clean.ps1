# Scripts helper for the ECommerce repo on Windows.
# Stops leftover dev-server / test / build processes that can lock bin\Debug
# outputs during builds (the classic MSB3027 "used by another process" error),
# shuts down persistent .NET build servers, and optionally rebuilds.

[CmdletBinding()]
param(
    [switch]$Build,
    [switch]$NoBuildServerShutdown
)

$ErrorActionPreference = 'Continue'

Write-Host '=== ECommerce dev cleanup ===' -ForegroundColor Cyan

# 1. Apps launched as their own executable (e.g. F5 / dotnet run of ECommerce.Api).
foreach ($name in 'ECommerce.Api', 'ECommerce') {
    Get-Process -Name $name -ErrorAction SilentlyContinue | ForEach-Object {
        Write-Host "Stopping app process: $($_.ProcessName) (PID $($_.Id))" -ForegroundColor Yellow
        Stop-Process -Id $_.Id -Force
    }
}

# 2. dotnet.exe hosting project binaries: 'dotnet run/watch' of this repo or an
#    earlier agent/IDE worktree copy (e.g. %TEMP%\ECommerce-*). Identified by
#    command line, so unrelated projects and IDE language servers are untouched.
$dotnetHosts = Get-CimInstance Win32_Process -Filter "Name='dotnet.exe' OR Name='testhost.exe'" |
    Where-Object {
        $cmd = $_.CommandLine
        $null -ne $cmd -and $cmd -match 'ECommerce' -and (
            $cmd -match 'ECommerce\.Api\.dll' -or
            $cmd -match '(dotnet|Expect\.exe) "?\S*(run|watch)"?' -or
            $cmd -match 'bin\\(Debug|Release)' -or
            $cmd -match 'testhost\.exe'
        )
    }

foreach ($proc in $dotnetHosts) {
    Write-Host "Stopping hosting process: $($proc.Name) (PID $($proc.ProcessId))" -ForegroundColor Yellow
    Write-Host "    $($proc.CommandLine)"
    Stop-Process -Id $proc.ProcessId -Force -ErrorAction SilentlyContinue
}

# 3. Shut down persistent MSBuild/Roslyn build servers: stale ones lock obj\ files.
if (-not $NoBuildServerShutdown) {
    Write-Host 'Shutting down dotnet build servers...' -ForegroundColor Yellow
    cmd /c "dotnet build-server shutdown" | Out-Host
}

if ($Build) {
    Write-Host 'Rebuilding solution...' -ForegroundColor Cyan
    dotnet build ECommerce.slnx --nologo
}
else {
    Write-Host 'Done. Run this script again with -Build to rebuild in one step.'
}
