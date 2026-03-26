using Avalonia.Media;
using Calendar.App.Support;

namespace Calendar.App.ViewModels;

public sealed class EventListItemViewModel
{
    public EventListItemViewModel(
        string id,
        string title,
        string categoryName,
        string colorHex,
        string dateText,
        string notes,
        bool isSelected,
        bool isDarkMode)
    {
        Id = id;
        Title = title;
        CategoryName = categoryName;
        DateText = dateText;
        Notes = notes;
        AccentBrush = BrushFactory.FromHex(colorHex);
        SurfaceBrush = BrushFactory.ListSurface(isDarkMode, isSelected);
        ForegroundBrush = BrushFactory.PrimaryText(isDarkMode);
        MutedForegroundBrush = BrushFactory.MutedText(isDarkMode);
    }

    public string Id { get; }

    public string Title { get; }

    public string CategoryName { get; }

    public string DateText { get; }

    public string Notes { get; }

    public IBrush AccentBrush { get; }

    public IBrush SurfaceBrush { get; }

    public IBrush ForegroundBrush { get; }

    public IBrush MutedForegroundBrush { get; }
}
