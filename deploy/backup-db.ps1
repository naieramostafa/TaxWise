# Backup automation for Streamline-Tax-And-Compliance (Windows)
# Usage: .\deploy\backup-db.ps1
# Schedule via Task Scheduler: schtasks /create /tn "StreamlineTaxBackup" /tr "powershell -File D:\Streamline-Tax-And-Compliance\deploy\backup-db.ps1" /sc daily /st 02:00

param(
    [string]$OutputDir = ".\backups"
)

$ErrorActionPreference = "Stop"
$env:PGPASSWORD = "postgres"
$dbName = "streamline_tax"
$dbUser = "postgres"
$dbHost = "localhost"
$dbPort = "5432"

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null
$stamp = Get-Date -Format "yyyyMMdd_HHmmss"
$dumpFile = Join-Path $OutputDir "$dbName`_$stamp.dump"

# Plain SQL dump via pg_dump
& "pg_dump" -h $dbHost -p $dbPort -U $dbUser -d $dbName -F c -f $dumpFile

# Compress with gzip (Windows tar supports gzip)
& "tar" -czf "$dumpFile.gz" -C $OutputDir "$(Split-Path $dumpFile -Leaf)"
Remove-Item $dumpFile

# Keep only last 14 backups
Get-ChildItem $OutputDir -Filter "$dbName`_*.dump.gz" |
    Sort-Object LastWriteTime -Descending |
    Select-Object -Skip 14 |
    Remove-Item

Write-Host "Backup created: $dumpFile.gz"
