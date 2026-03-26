using System.Collections.ObjectModel;
using Avalonia.Media;
using Calendar.App.Support;
using Calendar.Core.Domain;

namespace Calendar.App.ViewModels;

public sealed class MonthCellViewModel
{
    public MonthCellViewModel(
        SolDate date,
        string title,
        string subtitle,
        string gregorianLabel,
        bool isSelected,
        bool isToday,
        IEnumerable<EventBadgeViewModel> events,
        IEnumerable<OverflowDotViewModel> overflowDots,
        bool isDarkMode)
    {
        Date = date;
        Title = title;
        Subtitle = subtitle;
        GregorianLabel = gregorianLabel;
        Events = new ObservableCollection<EventBadgeViewModel>(events);
        OverflowDots = new ObservableCollection<OverflowDotViewModel>(overflowDots);
        SurfaceBrush = BrushFactory.Surface(isDarkMode, isSelected);
        BorderBrush = BrushFactory.Border(isDarkMode, isToday);
        ForegroundBrush = BrushFactory.PrimaryText(isDarkMode);
        MutedForegroundBrush = BrushFactory.MutedText(isDarkMode);
    }

    public SolDate Date { get; }

    public string Title { get; }

    public string Subtitle { get; }

    public string GregorianLabel { get; }

    public ObservableCollection<EventBadgeViewModel> Events { get; }

    public ObservableCollection<OverflowDotViewModel> OverflowDots { get; }

    public bool HasOverflowDots => OverflowDots.Count > 0;

    public IBrush SurfaceBrush { get; }

    public IBrush BorderBrush { get; }

    public IBrush ForegroundBrush { get; }

    public IBrush MutedForegroundBrush { get; }
}
