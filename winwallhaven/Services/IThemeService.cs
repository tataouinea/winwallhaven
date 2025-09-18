namespace winwallhaven.Services;

public enum AppTheme
{
    System,
    Light,
    Dark
}

public interface IThemeService
{
    AppTheme CurrentTheme { get; }
    void ApplyTheme(AppTheme theme);
}