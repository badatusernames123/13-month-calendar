using Avalonia.Media;
using Calendar.App.Support;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Calendar.App.ViewModels;

public sealed partial class CategoryFilterItemViewModel : ViewModelBase
{
    private readonly Action<CategoryFilterItemViewModel> _selectionChanged;

    public CategoryFilterItemViewModel(string id, string name, string colorHex, bool isSelected, Action<CategoryFilterItemViewModel> selectionChanged)
    {
        Id = id;
        Name = name;
        ColorHex = colorHex;
        AccentBrush = BrushFactory.FromHex(colorHex);
        _selectionChanged = selectionChanged;
        this.isSelected = isSelected;
    }

    public string Id { get; }

    public string Name { get; }

    public string ColorHex { get; }

    public IBrush AccentBrush { get; }

    [ObservableProperty]
    private bool isSelected;

    partial void OnIsSelectedChanged(bool value)
    {
        _selectionChanged(this);
    }
}
