# FileOrganixr

FileOrganixr is a Windows desktop app that watches folders and executes file actions based on YAML rules.
It supports approval-gated actions, real-time request tracking, and architecture-specific release packaging (`win-x64`, `win-arm64`).

## What It Does

- Watches one or more folders for new/changed files.
- Matches files against configured rules.
- Executes actions (`Move`, `Delete`, `Rename`) when rules match.
- Supports optional user approval before execution.
- Shows live request history and statuses in UI.

## Requirements

For end users:

- Windows 10/11
- .NET Desktop Runtime 10

For developers/build engineers:

- Windows 10/11
- .NET SDK 10
- PowerShell (`pwsh`) or Windows PowerShell

## Install and Run (Release Artifacts)

Release outputs are generated under:

- `artifacts/release/v<version>/win-x64/`
- `artifacts/release/v<version>/win-arm64/`

If MSIX/AppX is not emitted by your environment, ZIP fallback artifacts are generated:

- `FileOrganixr-v<version>-win-x64.zip`
- `FileOrganixr-v<version>-win-arm64.zip`

### Option A: ZIP Fallback Artifacts

1. Extract the ZIP for your architecture.
2. Run `FileOrganixr.UI.exe`.

### Option B: MSIX/AppX Artifacts (if available)

Install from PowerShell:

```powershell
Add-AppxPackage "path\to\your-package.msix"
```

## Quick Start Configuration

FileOrganixr uses a YAML config file. The runtime resolves config path in this order:

1. `FILEORGANIXR_CONFIG_PATH` environment variable (if set)
2. Settings file value (`%APPDATA%\FileOrganixr\settings.json`)
3. Default config path (`%USERPROFILE%\Documents\FileOrganixr\config.yaml`)

### Minimal Example

```yaml
SchemaVersion: 1
Folders:
  - Name: Inbox
    Path: 'C:\Users\you\Desktop\Inbox'
    Rules:
      - Name: Move Images
        UserApproval: false
        Query:
          Type: RegexFileName
          Pattern: '.*\.(png|jpg|jpeg)$'
          IgnoreCase: true
        Action:
          Type: Move
          DestinationPath: 'C:\Users\you\Pictures\FromInbox'
```

### Supported Query Types

- `RegexFileName`
  - `Pattern` (required)
  - `IgnoreCase` (optional, default `false`)
- `FileSize`
  - `MinSize` (optional, default `0`)
  - `MaxSize` (optional, default `decimal.MaxValue`)

### Supported Action Types

- `Move`
  - `DestinationPath` (required)
- `Delete`
  - no additional fields
- `Rename`
  - `Pattern` (required; target file name)

## Run From Source

From repo root:

```powershell
dotnet run --project .\src\FileOrganixr.UI\FileOrganixr.UI.csproj
```

## Build and Package

### Unsigned MSIX Build

Both targets:

```powershell
pwsh .\scripts\windows\publish-msix.ps1 -Target all
```

Single target:

```powershell
pwsh .\scripts\windows\publish-msix.ps1 -Target arm64
pwsh .\scripts\windows\publish-msix.ps1 -Target x64
```

### Versioned Release Package Build

Both targets:

```powershell
pwsh .\scripts\windows\build-release-package.ps1 -Version 1.0.0 -Target all
```

Single target:

```powershell
pwsh .\scripts\windows\build-release-package.ps1 -Version 1.0.0 -Target arm64
pwsh .\scripts\windows\build-release-package.ps1 -Version 1.0.0 -Target x64
```

From `cmd.exe`:

```cmd
powershell -ExecutionPolicy Bypass -File .\scripts\windows\build-release-package.ps1 -Version 1.0.0 -Target all
```

## Verify Release Integrity

Release build generates:

- `artifacts/release/v<version>/SHA256SUMS.txt`
- `artifacts/release/v<version>/release-manifest.json`

Verify a package hash:

```powershell
Get-FileHash .\artifacts\release\v1.0.0\FileOrganixr-v1.0.0-win-x64.zip -Algorithm SHA256
Get-Content .\artifacts\release\v1.0.0\SHA256SUMS.txt
```

`release-manifest.json` includes:

- `version`
- `configuration`
- `generatedAtUtc`
- artifact entries (`runtimeIdentifier`, `file`, `sizeBytes`, `sha256`)

## How to Use the App

After startup:

1. The app loads configuration and starts host services.
2. File events produce action requests in the main list.
3. Each request transitions through statuses (for example: `Detected -> RuleMatched -> Queued -> Processing -> Completed`).
4. If a rule has `UserApproval: true`, request waits in `PendingApproval`.
5. Use UI approve/reject actions to continue or reject execution.

## Troubleshooting

### Startup Error Dialog at Launch

Likely causes:

- Config file path is invalid or missing.
- YAML content is invalid.
- Folder paths in config are missing/incorrect.

Fix:

- Set `FILEORGANIXR_CONFIG_PATH` to a known-good YAML file path.
- Re-run the app.

### No Files Are Being Processed

Check:

- Watched folder paths exist.
- Rules actually match incoming files.
- File events are direct children of configured folder path.

### `win-x64` Packaging Fails on Windows ARM64

Ensure x64 SDK host exists:

```powershell
"C:\Program Files\dotnet\x64\dotnet.exe" --info
```

If missing, install x64 .NET SDK.

### No MSIX/AppX Produced

This can happen in some environments. Release script will automatically generate ZIP fallback artifacts per architecture.

## Current Deployment Scope

Current release flow is intentionally unsigned and does not configure AppInstaller feed/update URL yet.
Those are planned follow-up improvements after baseline packaging and release automation.

## For Developers: Extend Actions and Queries

The extension model has two separate pipelines:

- Query pipeline: YAML `Query` -> `QueryDefinitionRegistry` -> `IQueryDefinitionValidator` -> `IQueryMatcher` -> rule matching.
- Action pipeline: YAML `Action` -> `ActionDefinitionRegistry` -> `IActionDefinitionValidator` -> `IActionHandler` -> execution.

### Add a New Query Type (Step by Step)

1. Create a new query definition class in core.
   - Add a new file under `src/FileOrganixr.Core/Configuration/Definitions/Queries/`.
   - Inherit from `QueryDefinition` and set the type discriminator in the base constructor.
2. Add parsing support in `QueryDefinitionRegistry`.
   - Update `src/FileOrganixr.Core/Configuration/Definitions/Registries/QueryDefinitionRegistry.cs`.
   - Add a new `if` branch for your query `type` and map YAML args to your definition properties.
3. Add config validation for the new query.
   - Create `IQueryDefinitionValidator` implementation under `src/FileOrganixr.Core/Configuration/Validators/QueryValidator/`.
   - Return `SupportedType` that matches your query `Type`.
4. Register validator and matcher in DI.
   - Update `src/FileOrganixr.Core/CoreServicesContainerRegistrar.cs`.
   - Register your validator as `IQueryDefinitionValidator`.
   - Register your matcher as `IQueryMatcher`.
5. Create runtime matcher logic.
   - Add `IQueryMatcher` implementation under `src/FileOrganixr.Core/Runtime/Queries/`.
   - Set `SupportedQueryType` to your new definition type and implement `IsMatch`.
6. Add unit tests.
   - Add tests for:
     - Registry mapping (`QueryDefinitionRegistry`).
     - Validator behavior (valid/invalid cases).
     - Matcher behavior (matching and non-matching cases).
   - Existing tests in `tests/FileOrganixr.Tests/Runtime/Queries/` and `tests/FileOrganixr.Tests/Configuration/Validators/` are the best templates.
7. Use the new query in YAML.
   - Example shape:

```yaml
Query:
  Type: YourQueryType
  YourProperty: SomeValue
```

### Add a New Action Type (Step by Step)

1. Create a new action definition class in core.
   - Add a file under `src/FileOrganixr.Core/Configuration/Definitions/Actions/`.
   - Inherit from `ActionDefinition` and set the type discriminator.
2. Add parsing support in `ActionDefinitionRegistry`.
   - Update `src/FileOrganixr.Core/Configuration/Definitions/Registries/ActionDefinitionRegistry.cs`.
   - Add a new `if` branch for your action `type` and map YAML args.
3. Add config validation for the new action.
   - Create `IActionDefinitionValidator` implementation under `src/FileOrganixr.Core/Configuration/Validators/ActionValidators/`.
   - Set `SupportedType` to match your action `Type`.
4. Register validator in DI.
   - Update `src/FileOrganixr.Core/CoreServicesContainerRegistrar.cs`.
   - Register your validator as `IActionDefinitionValidator`.
5. Implement execution handler in infrastructure.
   - Add `IActionHandler` implementation under `src/FileOrganixr.Infrastructure/Execution/`.
   - Set `SupportedActionType` to your action type (case-insensitive match is supported).
   - Implement `ExecuteAsync(ActionRequest request, IActionDefinition actionDefinition, CancellationToken cancellationToken)`.
6. Register handler in infrastructure DI.
   - Update `src/FileOrganixr.Infrastructure/InfrastructureServicesRegistrar.cs`.
   - Register your handler as `IActionHandler`.
7. Add unit tests.
   - Add tests for:
     - Registry mapping (`ActionDefinitionRegistry`).
     - Validator behavior.
     - Handler behavior.
     - Executor resolution path (`ActionExecutor` + `ActionHandlerRegistry`).
   - Existing tests in `tests/FileOrganixr.Tests/Execution/` and `tests/FileOrganixr.Tests/Configuration/Validators/` are the best templates.
8. Use the new action in YAML.
   - Example shape:

```yaml
Action:
  Type: YourActionType
  YourProperty: SomeValue
```

### End-to-End Checklist for New Types

1. Add definition class.
2. Add registry mapping.
3. Add validator + DI registration.
4. Add matcher (query) or handler (action) + DI registration.
5. Add YAML sample to config.
6. Add unit tests for parser, validator, and runtime behavior.
7. Run unit tests on Windows:

```powershell
dotnet test .\tests\FileOrganixr.Tests\FileOrganixr.Tests.csproj
```
