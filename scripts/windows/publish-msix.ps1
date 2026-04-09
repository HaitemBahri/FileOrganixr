param(
    [ValidateSet("Release", "Debug")]
    [string]$Configuration = "Release",

    [ValidateSet("all", "x64", "arm64")]
    [string]$Target = "all"
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).ProviderPath
$projectPath = Join-Path $repoRoot "src\FileOrganixr.UI\FileOrganixr.UI.csproj"

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

$targets = switch ($Target) {
    "x64" { @(@{ RuntimeIdentifier = "win-x64"; PublishProfile = "MSIX-x64" }) }
    "arm64" { @(@{ RuntimeIdentifier = "win-arm64"; PublishProfile = "MSIX-arm64" }) }
    default {
        @(
            @{ RuntimeIdentifier = "win-x64"; PublishProfile = "MSIX-x64" },
            @{ RuntimeIdentifier = "win-arm64"; PublishProfile = "MSIX-arm64" }
        )
    }
}

foreach ($item in $targets) {
    $rid = $item.RuntimeIdentifier
    $profile = $item.PublishProfile
    $dotnetExe = Resolve-DotnetExeForRuntimeIdentifier -RuntimeIdentifier $rid

    Write-Host "Publishing unsigned MSIX ($rid)..."
    & $dotnetExe publish $projectPath -c $Configuration -p:PublishProfile=$profile

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed for '$rid' (profile: $profile)."
    }
}

Write-Host "Done. Packages are under artifacts/msix/."
