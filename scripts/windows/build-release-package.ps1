param(
    [Parameter(Mandatory = $true)]
    [string]$Version,

    [ValidateSet("Release", "Debug")]
    [string]$Configuration = "Release",

    [ValidateSet("all", "x64", "arm64")]
    [string]$Target = "all"
)

$ErrorActionPreference = "Stop"

function Assert-Windows {
    $isWindows = [System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform(
        [System.Runtime.InteropServices.OSPlatform]::Windows)

    if (-not $isWindows) {
        throw "Release packaging is supported only on Windows."
    }
}

function Invoke-DotnetPublish {
    param(
        [string]$DotnetExe,
        [string]$ProjectPath,
        [string]$PublishProfile,
        [string]$BuildConfiguration,
        [string]$PublishDirectory
    )

    & $DotnetExe publish $ProjectPath `
        -c $BuildConfiguration `
        -p:PublishProfile=$PublishProfile `
        -p:PublishDir="$PublishDirectory" `
        -p:AppxPackageDir="$PublishDirectory"

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed for profile '$PublishProfile' (exit code $LASTEXITCODE)."
    }
}

function Resolve-DotnetExeForRuntimeIdentifier {
    param(
        [string]$RuntimeIdentifier
    )

    if ($RuntimeIdentifier -eq "win-x64") {
        $x64Dotnet = Join-Path $env:ProgramFiles "dotnet\x64\dotnet.exe"
        if (Test-Path $x64Dotnet) {
            return $x64Dotnet
        }

        throw @"
Building win-x64 on Windows ARM64 requires an x64 .NET SDK host.
Install x64 .NET SDK so this path exists:
  $x64Dotnet
"@
    }

    return "dotnet"
}

function Get-RelativePathCompat {
    param(
        [string]$BasePath,
        [string]$TargetPath
    )

    $normalizedBase = [System.IO.Path]::GetFullPath($BasePath).TrimEnd('\', '/')
    $normalizedTarget = [System.IO.Path]::GetFullPath($TargetPath)

    $baseWithSeparator = $normalizedBase + [System.IO.Path]::DirectorySeparatorChar
    if ($normalizedTarget.StartsWith($baseWithSeparator, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $normalizedTarget.Substring($baseWithSeparator.Length).Replace('\', '/')
    }

    $baseUri = New-Object System.Uri(($normalizedBase.TrimEnd('\') + '\'))
    $targetUri = New-Object System.Uri($normalizedTarget)
    $relativeUri = $baseUri.MakeRelativeUri($targetUri)
    return [System.Uri]::UnescapeDataString($relativeUri.ToString()).Replace('\', '/')
}

Assert-Windows

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).ProviderPath
$projectPath = Join-Path $repoRoot "src\FileOrganixr.UI\FileOrganixr.UI.csproj"
$releaseRoot = Join-Path $repoRoot "artifacts\release\v$Version"

$targets = switch ($Target) {
    "x64" {
        @(
            @{ RuntimeIdentifier = "win-x64"; Profile = "MSIX-x64" }
        )
    }
    "arm64" {
        @(
            @{ RuntimeIdentifier = "win-arm64"; Profile = "MSIX-arm64" }
        )
    }
    default {
        @(
            @{ RuntimeIdentifier = "win-x64"; Profile = "MSIX-x64" },
            @{ RuntimeIdentifier = "win-arm64"; Profile = "MSIX-arm64" }
        )
    }
}

if (Test-Path $releaseRoot) {
    Remove-Item -Path $releaseRoot -Recurse -Force
}

New-Item -Path $releaseRoot -ItemType Directory -Force | Out-Null

$artifacts = New-Object System.Collections.Generic.List[object]

foreach ($targetSpec in $targets) {
    $rid = $targetSpec.RuntimeIdentifier
    $profile = $targetSpec.Profile
    $ridOutputDir = Join-Path $releaseRoot $rid

    New-Item -Path $ridOutputDir -ItemType Directory -Force | Out-Null

    Write-Host "Publishing $rid ($profile)..."
    $dotnetExe = Resolve-DotnetExeForRuntimeIdentifier -RuntimeIdentifier $rid
    Invoke-DotnetPublish `
        -DotnetExe $dotnetExe `
        -ProjectPath $projectPath `
        -PublishProfile $profile `
        -BuildConfiguration $Configuration `
        -PublishDirectory $ridOutputDir

    $packageFiles = Get-ChildItem -Path $ridOutputDir -File -Recurse |
        Where-Object { $_.Extension -in @(".msix", ".msixbundle", ".appx", ".appxbundle") }

    if ($packageFiles.Count -eq 0) {
        Write-Warning "No MSIX/AppX artifacts were produced for '$rid'. Creating ZIP fallback release package."

        $zipFileName = "FileOrganixr-v$Version-$rid.zip"
        $zipPath = Join-Path $releaseRoot $zipFileName

        if (Test-Path $zipPath) {
            Remove-Item -Path $zipPath -Force
        }

        Compress-Archive -Path (Join-Path $ridOutputDir "*") -DestinationPath $zipPath -Force

        $packageFiles = @(Get-Item $zipPath)
    }

    foreach ($package in $packageFiles) {
        $hash = (Get-FileHash -Path $package.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        $relativePath = Get-RelativePathCompat -BasePath $releaseRoot -TargetPath $package.FullName

        $artifacts.Add(
            [PSCustomObject]@{
                runtimeIdentifier = $rid
                file = $relativePath
                sizeBytes = $package.Length
                sha256 = $hash
            }
        ) | Out-Null
    }
}

$checksumFilePath = Join-Path $releaseRoot "SHA256SUMS.txt"
$checksumLines = $artifacts |
    Sort-Object file |
    ForEach-Object { "$($_.sha256)  $($_.file)" }

Set-Content -Path $checksumFilePath -Value $checksumLines

$manifestFilePath = Join-Path $releaseRoot "release-manifest.json"
$manifest = [PSCustomObject]@{
    version = $Version
    configuration = $Configuration
    generatedAtUtc = (Get-Date).ToUniversalTime().ToString("O")
    artifacts = $artifacts
}

$manifest | ConvertTo-Json -Depth 8 | Set-Content -Path $manifestFilePath

Write-Host "Release package output: $releaseRoot"
Write-Host "Generated: SHA256SUMS.txt, release-manifest.json"
