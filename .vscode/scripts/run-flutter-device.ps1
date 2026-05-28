# .vscode/scripts/run-flutter-device.ps1
# run-flutter-device.ps1
# Auto-detects your current WiFi/LAN IP and runs Flutter with it.
# Place this in .vscode/scripts/

# ── Get the active LAN IP (prefers WiFi, falls back to Ethernet) ──────────────
$ip = $null

# Try WiFi first
$wifi = Get-NetIPAddress -AddressFamily IPv4 |
        Where-Object {
            $_.InterfaceAlias -match 'Wi-Fi|WiFi|Wireless|WLAN' -and
            $_.IPAddress -notmatch '^(127\.|169\.254\.|0\.)'
        } |
        Select-Object -First 1 -ExpandProperty IPAddress

if ($wifi) {
    $ip = $wifi
} else {
    # Fallback to any active non-loopback IPv4
    $ip = Get-NetIPAddress -AddressFamily IPv4 |
          Where-Object {
              $_.IPAddress -notmatch '^(127\.|169\.254\.|0\.)' -and
              $_.PrefixOrigin -ne 'WellKnown'
          } |
          Select-Object -First 1 -ExpandProperty IPAddress
}

if (-not $ip) {
    Write-Error "Could not detect a LAN IP address. Is WiFi connected?"
    exit 1
}

$apiUrl = "http://${ip}:5181"
Write-Host "Detected LAN IP: $ip" -ForegroundColor Cyan
Write-Host "API_BASE_URL   : $apiUrl" -ForegroundColor Cyan
Write-Host ""

# ── Run Flutter with the detected IP ──────────────────────────────────────────
$flutterScript = Join-Path $PSScriptRoot "flutter-with-git.cmd"

& $flutterScript run `
    --dart-define "API_BASE_URL=$apiUrl" `
    @args