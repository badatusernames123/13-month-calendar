using Avalonia.Media;
using Calendar.App.Support;

namespace Calendar.App.ViewModels;

public sealed class EventBadgeViewModel
{
    public EventBadgeViewModel(string id, string title, string subtitle, string colorHex, bool isDarkMode)
    {
        Id = id;
        Title = title;
        Subtitle = subtitle;
        HasEvent = true;
        AccentBrush = BrushFactory.FromHex(colorHex);
        BorderBrush = AccentBrush;
        BackgroundBrush = BrushFactory.BadgeBackground(isDarkMode);
        ForegroundBrush = BrushFactory.PrimaryText(isDarkMode);
        MutedForegroundBrush = BrushFactory.MutedText(isDarkMode);
        ChromeOpacity = 1;
    }

    private EventBadgeViewModel(bool isDarkMode)
    {
        Id = string.Empty;
        Title = string.Empty;
        Subtitle = string.Empty;
        HasEvent = false;
        AccentBrush = BrushFactory.Border(isDarkMode, false);
        BorderBrush = BrushFactory.Border(isDarkMode, false);
        BackgroundBrush = BrushFactory.BadgeBackground(isDarkMode);
        ForegroundBrush = BrushFactory.PrimaryText(isDarkMode);
        MutedForegroundBrush = BrushFactory.MutedText(isDarkMode);
        ChromeOpacity = 0.32;
    }

    public static EventBadgeViewModel Placeholder(bool isDarkMode) => new(isDarkMode);

    public string Id { get; }

    public string Title { get; }

    public string Subtitle { get; }

    public bool HasEvent { get; }

    public IBrush AccentBrush { get; }

    public IBrush BorderBrush { get; }

    public IBrush BackgroundBrush { get; }

    public IBrush ForegroundBrush { get; }

    public IBrush MutedForegroundBrush { get; }

    public double ChromeOpacity { get; }
}
