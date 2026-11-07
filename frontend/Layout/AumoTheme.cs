using MudBlazor;

namespace AumoFinance.Layout.Theme
{
    public static class AumoTheme
    {
        public static MudTheme Theme => new MudTheme()
        {
            PaletteLight = new PaletteLight()
            {
                Primary = "#2563EB",
                Secondary = "#6D28D9",
                Info = "#0891B2",
                Success = "#16A34A",
                Warning = "#D97706",
                Error = "#DC2626",
                Background = "#FFFFFF",
                Surface = "#FFFFFF",
                AppbarBackground = "#FFFFFF",
                DrawerBackground = "#FFFFFF",
                TextPrimary = "#0F172A",
                TextSecondary = "#64748B",
                LinesDefault = "#E2E8F0"
            },
            PaletteDark = new PaletteDark()
            {
                Primary = "#3B82F6",          // --aumo-primary
                Secondary = "#7C3AED",        // --aumo-secondary
                Info = "#00C2FF",             // --aumo-accent
                Success = "#22C55E",          // --aumo-success
                Warning = "#F59E0B",          // --aumo-warning
                Error = "#EF4444",            // --aumo-error
                Background = "#0D1117",       // --aumo-bg
                Surface = "#161B22",          // --aumo-surface
                AppbarBackground = "#161B22",
                DrawerBackground = "#161B22",
                TextPrimary = "#FFFFFF",      // --aumo-text
                TextSecondary = "#94A3B8",    // --aumo-text-muted
                LinesDefault = "#2A313C",     // --aumo-border
                TableLines = "#2A313C",
                Divider = "#2A313C"
            },
            Typography = new Typography()
            {
                Default = new DefaultTypography()
                {
                    FontFamily = new[] { "Inter", "sans-serif" }
                },
                H1 = new H1Typography() { FontFamily = new[] { "Space Grotesk", "sans-serif" } },
                H2 = new H2Typography() { FontFamily = new[] { "Space Grotesk", "sans-serif" } },
                H3 = new H3Typography() { FontFamily = new[] { "Space Grotesk", "sans-serif" } },
                H4 = new H4Typography() { FontFamily = new[] { "Space Grotesk", "sans-serif" } },
                H5 = new H5Typography() { FontFamily = new[] { "Space Grotesk", "sans-serif" } },
                H6 = new H6Typography() { FontFamily = new[] { "Space Grotesk", "sans-serif" } }
            },
            LayoutProperties = new LayoutProperties()
            {
                DefaultBorderRadius = "8px"
            }
        };
    }
}
