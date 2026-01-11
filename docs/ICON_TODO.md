# Package Icon TODO

## Task: Create Package Icon for NuGet

A 128x128 PNG icon is needed for the NuGet package.

### Requirements:
- Size: 128x128 pixels
- Format: PNG
- Location: `src/Mcp.TaskAndResearch/icon.png`

### Design Suggestions:
- Simple, clean design representing task management
- Use colors that work on both light and dark backgrounds
- Could include: checkmark, list, task symbols

### To Enable:
Once the icon file is created, add to `.csproj`:
```xml
<PackageIcon>icon.png</PackageIcon>
```

And add to the README ItemGroup:
```xml
<None Include="icon.png" Pack="true" PackagePath="\" />
```

### Alternative:
NuGet packages without icons will display a default placeholder. The package will still work perfectly without a custom icon.
