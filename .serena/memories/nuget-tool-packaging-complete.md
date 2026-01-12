# NuGet Global Tool Packaging - Completed

## Summary
Successfully converted mcp-task-and-research into a globally installable .NET tool via NuGet.org.

## Current State
- **Version**: 1.0.6 (published to NuGet.org)
- **Package URL**: https://www.nuget.org/packages/Mcp.TaskAndResearch
- **GitHub**: https://github.com/d-german/mcp-task-and-research
- **Branch**: All changes merged to `main`

## Version History
- 1.0.0 - Initial NuGet release
- 1.0.1 - Added --help flag with setup instructions
- 1.0.2 - Added --help mention to README
- 1.0.3 - Enhanced README with Shrimp origin, features, use cases
- 1.0.5 - Fixed: wwwroot static files now included in NuGet tool package
- 1.0.6 - Fixed: Path resolution for global tools, MudBlazor CDN for better compatibility

## Key Fixes for Global Tool Compatibility (v1.0.5-1.0.6)

### Issue 1: wwwroot Missing from Package
**Problem**: The `wwwroot` folder was empty in the NuGet tool package, causing Blazor UI to not function.
**Solution**: Added MSBuild target in .csproj to include wwwroot from build output:
```xml
<Target Name="AddWwwrootToToolPackage" AfterTargets="Build" BeforeTargets="GenerateNuspec">
  <ItemGroup>
    <TfmSpecificPackageFile Include="$(OutputPath)wwwroot\**\*">
      <PackagePath>tools\$(TargetFramework)\any\wwwroot\%(RecursiveDir)%(Filename)%(Extension)</PackagePath>
    </TfmSpecificPackageFile>
  </ItemGroup>
</Target>
```

### Issue 2: Assembly.Location Returns Empty for Global Tools
**Problem**: `Assembly.GetExecutingAssembly().Location` returns empty string for global .NET tools.
**Solution**: Changed to use `AppContext.BaseDirectory` which works correctly:
- ServerHost.cs: `var assemblyDir = AppContext.BaseDirectory;`
- PromptTemplateLoader.cs: `return AppContext.BaseDirectory;`

### Issue 3: MudBlazor Static Assets Not Available
**Problem**: MudBlazor assets referenced via `_content/MudBlazor/` require static web assets manifest which points to NuGet cache paths that don't exist in global tool context.
**Solution**: Changed to use CDN for MudBlazor assets in App.razor:
- `https://cdn.jsdelivr.net/npm/mudblazor@8.0.0/MudBlazor.min.css`
- `https://cdn.jsdelivr.net/npm/mudblazor@8.0.0/MudBlazor.min.js`

## Future Enhancements
- Add package icon (see docs/ICON_TODO.md)
- Consider GitHub Actions for automated publishing
