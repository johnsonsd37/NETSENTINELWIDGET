$ErrorActionPreference = 'Stop'
dotnet publish "$PSScriptRoot\NetSentinelWidget.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
Write-Host "Built: $PSScriptRoot\bin\Release\net8.0-windows\win-x64\publish\NetSentinelWidget.exe"
