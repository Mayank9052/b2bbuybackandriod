# .vscode/scripts/get-lan-ip.ps1
# get-lan-ip.ps1
# Prints your current LAN/WiFi IP. Run this anytime to verify.

$ip = Get-NetIPAddress -AddressFamily IPv4 |
      Where-Object {
          $_.IPAddress -notmatch '^(127\.|169\.254\.|0\.)' -and
          $_.PrefixOrigin -ne 'WellKnown'
      } |
      Sort-Object {
          if ($_.InterfaceAlias -match 'Wi-Fi|WiFi|Wireless|WLAN') { 0 } else { 1 }
      } |
      Select-Object -First 1

if ($ip) {
    Write-Host "Interface : $($ip.InterfaceAlias)" -ForegroundColor Green
    Write-Host "IP Address: $($ip.IPAddress)"      -ForegroundColor Green
    Write-Host ""
    Write-Host "Use this in API_BASE_URL: http://$($ip.IPAddress):5181" -ForegroundColor Cyan
} else {
    Write-Host "No active LAN IP found. Check WiFi connection." -ForegroundColor Red
}