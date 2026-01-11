# Suggested Commands

## Build Commands

### Build (Debug)
```powershell
dotnet build
```

### Build (Release)
```powershell
dotnet build -c Release
```

### Clean Build
```powershell
dotnet clean
dotnet build
```

## Run Commands

### Run from Project Directory
```powershell
cd src/Mcp.TaskAndResearch
dotnet run
```

### Run from Solution Root
```powershell
dotnet run --project src/Mcp.TaskAndResearch/Mcp.TaskAndResearch.csproj
```

### Run Release Build (Compiled Executable)
```powershell
# After building with -c Release
.\src\Mcp.TaskAndResearch\bin\Release\net9.0\Mcp.TaskAndResearch.exe
```

## Test Commands

### Run All Tests
```powershell
dotnet test
```

### Run Tests with Verbose Output
```powershell
dotnet test -v detailed
```

### Run Tests with Coverage
```powershell
dotnet test --collect:"XPlat Code Coverage"
```

## Publish Commands

### Publish for Windows (x64)
```powershell
dotnet publish -c Release -r win-x64 --self-contained false
```

### Publish for Linux (x64)
```powershell
dotnet publish -c Release -r linux-x64 --self-contained false
```

### Publish for macOS (Intel)
```powershell
dotnet publish -c Release -r osx-x64 --self-contained false
```

### Publish for macOS (Apple Silicon)
```powershell
dotnet publish -c Release -r osx-arm64 --self-contained false
```

Output location: `src/Mcp.TaskAndResearch/bin/Release/net9.0/{runtime}/publish/`

## Git Commands (Windows PowerShell)

### Check Status
```powershell
git status
```

### Stage Changes
```powershell
git add .
# or specific files
git add src/Mcp.TaskAndResearch/SomeFile.cs
```

### Commit Changes
```powershell
git commit -m "Your commit message"
```

### Push Changes
```powershell
git push
```

### Pull Latest Changes
```powershell
git pull
```

### Create New Branch
```powershell
git checkout -b feature/new-feature-name
```

### View Commit History
```powershell
git log --oneline
```

## File System Commands (Windows PowerShell)

### List Directory Contents
```powershell
Get-ChildItem
# or shorthand
dir
# or
ls
```

### Navigate Directories
```powershell
cd src/Mcp.TaskAndResearch
# Go up one level
cd ..
# Go to root
cd \
```

### Find Files
```powershell
Get-ChildItem -Recurse -Filter "*.cs"
```

### Search in Files (grep equivalent)
```powershell
Select-String -Path "*.cs" -Pattern "TaskStore" -Recurse
```

### View File Content
```powershell
Get-Content README.md
# or shorthand
cat README.md
```

## .NET SDK Commands

### Check .NET Version
```powershell
dotnet --version
```

### List Installed SDKs
```powershell
dotnet --list-sdks
```

### Restore NuGet Packages
```powershell
dotnet restore
```

### Add NuGet Package
```powershell
dotnet add package PackageName
```

### Update NuGet Package
```powershell
dotnet add package PackageName --version x.x.x
```

## Solution/Project Commands

### Add New Project to Solution
```powershell
dotnet sln add path/to/project.csproj
```

### List Projects in Solution
```powershell
dotnet sln list
```

### Create New Class Library
```powershell
dotnet new classlib -n ProjectName
```

### Create New xUnit Test Project
```powershell
dotnet new xunit -n ProjectName.Tests
```
