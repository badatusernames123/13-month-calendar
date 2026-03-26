using Avalonia.Media;

namespace Calendar.App.Support;

internal static class BrushFactory
{
    private static readonly SolidColorBrush DefaultBrush = new(Color.Parse("#22D3EE"));
    private static readonly SolidColorBrush LightPrimaryText = new(Color.Parse("#0F172A"));
    private static readonly SolidColorBrush LightMutedText = new(Color.Parse("#64748B"));
    private static readonly SolidColorBrush DarkPrimaryText = new(Color.Parse("#E6F7FF"));
    private static readonly SolidColorBrush DarkMutedText = new(Color.Parse("#8AA4C2"));

    public static SolidColorBrush FromHex(string? colorHex)
    {
        if (!string.IsNullOrWhiteSpace(colorHex) && Color.TryParse(colorHex, out var color))
        {
            return new SolidColorBrush(color);
        }

        return DefaultBrush;
    }

    public static SolidColorBrush PrimaryText(bool isDarkMode) => isDarkMode ? DarkPrimaryText : LightPrimaryText;

    public static SolidColorBrush MutedText(bool isDarkMode) => isDarkMode ? DarkMutedText : LightMutedText;

    public static SolidColorBrush Surface(bool isDarkMode, bool isSelected) =>
        FromHex(isDarkMode
            ? (isSelected ? "#11284A" : "#08111F")
            : (isSelected ? "#DBEAFE" : "#FFFFFF"));

    public static SolidColorBrush ListSurface(bool isDarkMode, bool isSelected) =>
        FromHex(isDarkMode
            ? (isSelected ? "#0E1C33" : "#09111E")
            : (isSelected ? "#E0F2FE" : "#F8FAFC"));

    public static SolidColorBrush Border(bool isDarkMode, bool isHighlighted) =>
        FromHex(isHighlighted
            ? "#22D3EE"
            : (isDarkMode ? "#1E3A5F" : "#CBD5E1"));

    public static SolidColorBrush BadgeBackground(bool isDarkMode) => FromHex(isDarkMode ? "#06101F" : "#F8FAFC");
}
