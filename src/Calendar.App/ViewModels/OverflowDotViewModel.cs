using Avalonia.Media;
using Calendar.App.Support;

namespace Calendar.App.ViewModels;

public sealed class OverflowDotViewModel
{
    public OverflowDotViewModel(string colorHex)
    {
        FillBrush = BrushFactory.FromHex(colorHex);
    }

    public IBrush FillBrush { get; }
}
