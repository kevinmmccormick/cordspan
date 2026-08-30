# Contributing to Cordspan

Cordspan welcomes focused bug reports, test results, documentation fixes, and
code contributions.

## Development requirements

- Windows 10 version 1809 or newer
- .NET SDK 10 or newer
- An x64 development environment
- `usbipd-win` and `usbip-win2` for end-to-end device testing

The USB/IP tools are not required for parser and service unit tests.

## Build and test

From the repository root:

```powershell
dotnet restore .\Cordspan.sln
dotnet test .\Cordspan.sln -c Release -p:Platform=x64
```

Run the application with:

```powershell
dotnet run --project .\src\Cordspan\Cordspan.csproj -p:Platform=x64
```

Windows requests administrator approval when the application starts.

## Pull requests

Keep changes focused and include tests for command construction, parsing, and
failure behavior where applicable. Before opening a pull request:

1. Build and run the complete test suite in Release configuration.
2. Confirm new UI text fits at normal Windows display scaling.
3. Avoid committing `bin`, `obj`, `.vs`, screenshots, credentials, USB/IP
   executables, or machine-local configuration.
4. Describe any manual USB hardware and network validation performed.

Use generic USB/IP language such as share, attach, detach, host, and client.
Do not describe the Windows-to-Windows workflow as a WSL workflow.

## Reporting problems

Include the Windows version, Cordspan version or commit, relevant tool versions,
the operation attempted, and the complete error text. Remove hostnames, IP
addresses, serial numbers, and other private data before posting logs.
