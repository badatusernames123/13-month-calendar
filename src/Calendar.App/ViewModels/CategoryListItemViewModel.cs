using Avalonia.Media;
using Calendar.App.Support;

namespace Calendar.App.ViewModels;

public sealed class CategoryListItemViewModel
{
    public CategoryListItemViewModel(string id, string name, string colorHex, int eventCount, bool isSelected, bool isDarkMode)
    {
        Id = id;
        Name = name;
        ColorHex = colorHex;
        EventCount = eventCount;
        AccentBrush = BrushFactory.FromHex(colorHex);
        SurfaceBrush = BrushFactory.ListSurface(isDarkMode, isSelected);
        ForegroundBrush = BrushFactory.PrimaryText(isDarkMode);
        MutedForegroundBrush = BrushFactory.MutedText(isDarkMode);
    }

    public string Id { get; }

    public string Name { get; }

    public string ColorHex { get; }

    public int EventCount { get; }

    public IBrush AccentBrush { get; }

    public IBrush SurfaceBrush { get; }

    public IBrush ForegroundBrush { get; }

    public IBrush MutedForegroundBrush { get; }
}
