using MudBlazor;

namespace Mcp.TaskAndResearch.UI.Theme;

/// <summary>
/// Custom MudBlazor theme configuration for the Task Manager application.
/// Provides consistent branding with light and dark mode support.
/// </summary>
public static class AppTheme
{
    public static readonly MudTheme Theme = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = Colors.Blue.Darken1,
            PrimaryDarken = Colors.Blue.Darken3,
            PrimaryLighten = Colors.Blue.Lighten1,
            Secondary = Colors.Teal.Default,
            SecondaryDarken = Colors.Teal.Darken2,
            Tertiary = Colors.Orange.Default,
            Info = Colors.LightBlue.Default,
            Success = Colors.Green.Default,
            Warning = Colors.Amber.Default,
            Error = Colors.Red.Default,
            Background = Colors.Gray.Lighten5,
            Surface = Colors.Shades.White,
            DrawerBackground = Colors.Shades.White,
            DrawerText = Colors.Gray.Darken3,
            DrawerIcon = Colors.Gray.Darken1,
            AppbarBackground = Colors.Blue.Darken1,
            AppbarText = Colors.Shades.White,
            TextPrimary = Colors.Gray.Darken3,
            TextSecondary = Colors.Gray.Default,
            ActionDefault = Colors.Gray.Darken1,
            ActionDisabled = Colors.Gray.Lighten2,
            ActionDisabledBackground = Colors.Gray.Lighten4,
            Divider = Colors.Gray.Lighten2,
            DividerLight = Colors.Gray.Lighten3,
            TableLines = Colors.Gray.Lighten2,
            TableStriped = Colors.Gray.Lighten5,
            TableHover = Colors.Blue.Lighten5,
            LinesDefault = Colors.Gray.Lighten2,
            LinesInputs = Colors.Gray.Lighten1
        },
        PaletteDark = new PaletteDark
        {
            Primary = Colors.Blue.Lighten1,
            PrimaryDarken = Colors.Blue.Default,
            PrimaryLighten = Colors.Blue.Lighten2,
            Secondary = Colors.Teal.Lighten1,
            SecondaryDarken = Colors.Teal.Default,
            Tertiary = Colors.Orange.Lighten1,
            Info = Colors.LightBlue.Lighten1,
            Success = Colors.Green.Lighten1,
            Warning = Colors.Amber.Lighten1,
            Error = Colors.Red.Lighten1,
            Background = "#1a1a2e",
            Surface = "#16213e",
            DrawerBackground = "#0f0f23",
            DrawerText = Colors.Gray.Lighten2,
            DrawerIcon = Colors.Gray.Lighten1,
            AppbarBackground = "#0f3460",
            AppbarText = Colors.Shades.White,
            TextPrimary = Colors.Gray.Lighten2,
            TextSecondary = Colors.Gray.Default,
            ActionDefault = Colors.Gray.Lighten1,
            ActionDisabled = Colors.Gray.Darken2,
            ActionDisabledBackground = Colors.Gray.Darken3,
            Divider = Colors.Gray.Darken2,
            DividerLight = Colors.Gray.Darken3,
            TableLines = Colors.Gray.Darken2,
            TableStriped = "#1e1e3f",
            TableHover = "#252550",
            LinesDefault = Colors.Gray.Darken2,
            LinesInputs = Colors.Gray.Darken1
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "6px",
            DrawerMiniWidthLeft = "56px",
            DrawerMiniWidthRight = "56px",
            DrawerWidthLeft = "260px",
            DrawerWidthRight = "260px"
        }
    };
}
