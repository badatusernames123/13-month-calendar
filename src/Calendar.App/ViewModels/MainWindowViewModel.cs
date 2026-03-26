using System.Collections.ObjectModel;
using Avalonia.Threading;
using Calendar.Core.Domain;
using Calendar.Core.Infrastructure;
using Calendar.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Calendar.App.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly CalendarRepository _repository;
    private readonly IClock _clock;
    private readonly DispatcherTimer _progressTimer;
    private readonly DispatcherTimer _autoRefreshTimer;
    private readonly HashSet<string> _selectedFilterIds = new(StringComparer.OrdinalIgnoreCase);

    private IReadOnlyList<CalendarCategory> _categories = [];
    private IReadOnlyList<CalendarEvent> _events = [];
    private string? _editingEventId;
    private bool _suppressFilterNotifications;
    private bool _isRefreshingFromDisk;

    public MainWindowViewModel(CalendarRepository repository, IClock clock)
    {
        _repository = repository;
        _clock = clock;

        var today = SolCalendarMath.FromGregorian(DateOnly.FromDateTime(clock.Now.DateTime));
        selectedDate = today;
        EventDateModes = ["Month Day", "Leap Day", "Year Day"];
        CategoryFilters = [];
        CategoryItems = [];
        EventCategoryOptions = [];
        MonthCells = [];
        WeekCells = [];
        SelectedDateEvents = [];

        PreviousCommand = new RelayCommand(MovePrevious);
        NextCommand = new RelayCommand(MoveNext);
        TodayCommand = new RelayCommand(MoveToday);
        SelectDateCommand = new RelayCommand<SolDate>(SelectDate);
        SelectEventCommand = new RelayCommand<string>(SelectEvent);
        StatusMessage = "Watching calendar data for updates.";

        _progressTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(30),
        };
        _progressTimer.Tick += (_, _) => RefreshProgress();
        _progressTimer.Start();

        _autoRefreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2),
        };
        _autoRefreshTimer.Tick += async (_, _) => await RefreshFromDiskAsync();
        _autoRefreshTimer.Start();

        RefreshProgress();
        RefreshSelectionSummaries();
    }

    public ObservableCollection<string> EventDateModes { get; }

    public ObservableCollection<CategoryFilterItemViewModel> CategoryFilters { get; }

    public ObservableCollection<CategoryListItemViewModel> CategoryItems { get; }

    public ObservableCollection<CategoryOptionViewModel> EventCategoryOptions { get; }

    public ObservableCollection<MonthCellViewModel> MonthCells { get; }

    public ObservableCollection<MonthCellViewModel> WeekCells { get; }

    public ObservableCollection<EventListItemViewModel> SelectedDateEvents { get; }

    public IRelayCommand PreviousCommand { get; }

    public IRelayCommand NextCommand { get; }

    public IRelayCommand TodayCommand { get; }

    public IRelayCommand<SolDate> SelectDateCommand { get; }

    public IRelayCommand<string> SelectEventCommand { get; }

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private int selectedViewIndex;

    [ObservableProperty]
    private SolDate selectedDate;

    [ObservableProperty]
    private string selectedDateLabel = string.Empty;

    [ObservableProperty]
    private string selectedGregorianLabel = string.Empty;

    [ObservableProperty]
    private string statusMessage = "Loading calendar data...";

    [ObservableProperty]
    private string dataFilePath = string.Empty;

    [ObservableProperty]
    private string monthViewTitle = string.Empty;

    [ObservableProperty]
    private string weekViewTitle = string.Empty;

    [ObservableProperty]
    private string dayViewTitle = string.Empty;

    [ObservableProperty]
    private string dayViewSubtitle = string.Empty;

    [ObservableProperty]
    private bool selectedDateIsSpecial;

    [ObservableProperty]
    private string specialDateDescription = string.Empty;

    [ObservableProperty]
    private double yearProgressValue;

    [ObservableProperty]
    private string yearProgressText = string.Empty;

    [ObservableProperty]
    private double monthProgressValue;

    [ObservableProperty]
    private string monthProgressText = string.Empty;

    [ObservableProperty]
    private double dayProgressValue;

    [ObservableProperty]
    private string dayProgressText = string.Empty;

    public async Task InitializeAsync()
    {
        await RefreshFromDiskAsync(forceStatusMessage: "Loaded calendar data.");
    }

    partial void OnSelectedDateChanged(SolDate value)
    {
        RefreshSelectionSummaries();
        BuildCalendarViews();
    }

    private void MovePrevious()
    {
        SelectedDate = SelectedViewIndex switch
        {
            0 => MoveMonth(-1),
            1 => SelectedDate.IsSpecialDay ? SolCalendarMath.AddDays(SelectedDate, -1) : SolCalendarMath.AddDays(SelectedDate, -7),
            _ => SolCalendarMath.AddDays(SelectedDate, -1),
        };
    }

    private void MoveNext()
    {
        SelectedDate = SelectedViewIndex switch
        {
            0 => MoveMonth(1),
            1 => SelectedDate.IsSpecialDay ? SolCalendarMath.AddDays(SelectedDate, 1) : SolCalendarMath.AddDays(SelectedDate, 7),
            _ => SolCalendarMath.AddDays(SelectedDate, 1),
        };
    }

    private SolDate MoveMonth(int direction)
    {
        if (SelectedDate.IsSpecialDay)
        {
            return SelectedDate.SpecialDayKind switch
            {
                SolSpecialDayKind.LeapDay when direction < 0 => new SolDate(SelectedDate.Year, 6, SolCalendarMath.DaysPerMonth),
                SolSpecialDayKind.LeapDay => new SolDate(SelectedDate.Year, 7, 1),
                SolSpecialDayKind.YearDay when direction < 0 => new SolDate(SelectedDate.Year, 13, SolCalendarMath.DaysPerMonth),
                SolSpecialDayKind.YearDay => new SolDate(SelectedDate.Year + 1, 1, 1),
                _ => SelectedDate,
            };
        }

        if (direction > 0)
        {
            if (SelectedDate.MonthNumber == 6 && SolCalendarMath.IsLeapYear(SelectedDate.Year))
            {
                return new SolDate(SelectedDate.Year, 0, 0, SolSpecialDayKind.LeapDay);
            }

            if (SelectedDate.MonthNumber == SolCalendarMath.MonthsPerYear)
            {
                return new SolDate(SelectedDate.Year, 0, 0, SolSpecialDayKind.YearDay);
            }
        }

        if (direction < 0)
        {
            if (SelectedDate.MonthNumber == 7 && SolCalendarMath.IsLeapYear(SelectedDate.Year))
            {
                return new SolDate(SelectedDate.Year, 0, 0, SolSpecialDayKind.LeapDay);
            }

            if (SelectedDate.MonthNumber == 1)
            {
                return new SolDate(SelectedDate.Year - 1, 0, 0, SolSpecialDayKind.YearDay);
            }
        }

        var targetMonth = SelectedDate.MonthNumber + direction;
        var targetYear = SelectedDate.Year;

        if (targetMonth < 1)
        {
            targetMonth = SolCalendarMath.MonthsPerYear;
            targetYear--;
        }
        else if (targetMonth > SolCalendarMath.MonthsPerYear)
        {
            targetMonth = 1;
            targetYear++;
        }

        return new SolDate(targetYear, targetMonth, Math.Min(SelectedDate.Day, SolCalendarMath.DaysPerMonth));
    }

    private void MoveToday()
    {
        SelectedDate = SolCalendarMath.FromGregorian(DateOnly.FromDateTime(_clock.Now.DateTime));
    }

    private void SelectDate(SolDate date) => SelectedDate = date;

    private void SelectEvent(string? eventId)
    {
        if (string.IsNullOrWhiteSpace(eventId))
        {
            return;
        }

        var calendarEvent = _events.FirstOrDefault(evt => string.Equals(evt.Id, eventId, StringComparison.OrdinalIgnoreCase));
        if (calendarEvent is null)
        {
            return;
        }

        _editingEventId = calendarEvent.Id;
        SelectedDate = calendarEvent.Date;
        BuildCalendarViews();
    }

    private void SynchronizeFilterSelection()
    {
        var knownIds = _categories.Select(category => category.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (_selectedFilterIds.Count == 0)
        {
            foreach (var id in knownIds)
            {
                _selectedFilterIds.Add(id);
            }

            return;
        }

        _selectedFilterIds.RemoveWhere(id => !knownIds.Contains(id));
        if (_selectedFilterIds.Count == 0)
        {
            foreach (var id in knownIds)
            {
                _selectedFilterIds.Add(id);
            }
        }
    }

    private void RebuildCategoryFilters()
    {
        _suppressFilterNotifications = true;
        CategoryFilters.Clear();

        foreach (var category in _categories)
        {
            CategoryFilters.Add(new CategoryFilterItemViewModel(
                category.Id,
                category.Name,
                category.ColorHex,
                _selectedFilterIds.Contains(category.Id),
                OnFilterSelectionChanged));
        }

        _suppressFilterNotifications = false;
    }

    private void RebuildCategoryItems(string? selectedCategoryId = null)
    {
        CategoryItems.Clear();

        foreach (var category in _categories)
        {
            CategoryItems.Add(new CategoryListItemViewModel(
                category.Id,
                category.Name,
                category.ColorHex,
                _events.Count(evt => string.Equals(evt.CategoryId, category.Id, StringComparison.OrdinalIgnoreCase)),
                false,
                IsDarkMode));
        }
    }

    private void OnFilterSelectionChanged(CategoryFilterItemViewModel item)
    {
        if (_suppressFilterNotifications)
        {
            return;
        }

        if (item.IsSelected)
        {
            _selectedFilterIds.Add(item.Id);
        }
        else
        {
            _selectedFilterIds.Remove(item.Id);
        }

        BuildCalendarViews();
    }

    private void RefreshSelectionSummaries()
    {
        SelectedDateLabel = SelectedDate.LongLabel;
        SelectedGregorianLabel = $"Gregorian reference: {SolCalendarMath.ToGregorianDate(SelectedDate):MMMM d, yyyy}";
        SelectedDateIsSpecial = SelectedDate.IsSpecialDay;
        SpecialDateDescription = SelectedDate.SpecialDayKind switch
        {
            SolSpecialDayKind.LeapDay => "Leap Day sits between June and Sol outside the month cycle.",
            SolSpecialDayKind.YearDay => "Year Day closes the year outside the month cycle.",
            _ => "Each month contains exactly four seven-day weeks.",
        };
    }

    private void BuildCalendarViews()
    {
        MonthViewTitle = SelectedDate.IsSpecialDay
            ? SelectedDate.LongLabel
            : $"{SelectedDate.MonthName} {SelectedDate.Year}";

        WeekViewTitle = SelectedDate.IsSpecialDay
            ? SelectedDate.LongLabel
            : $"Week {SolCalendarMath.GetWeekOfMonth(SelectedDate)} · {SelectedDate.MonthName} {SelectedDate.Year}";

        DayViewTitle = SelectedDate.LongLabel;
        DayViewSubtitle = SolCalendarMath.GetDayName(SelectedDate);

        MonthCells.Clear();
        if (!SelectedDate.IsSpecialDay)
        {
            foreach (var date in SolCalendarMath.GetMonthDays(SelectedDate.Year, SelectedDate.MonthNumber))
            {
                MonthCells.Add(BuildDateCell(date));
            }
        }

        WeekCells.Clear();
        foreach (var date in SolCalendarMath.GetWeekDates(SelectedDate))
        {
            WeekCells.Add(BuildDateCell(date));
        }

        SelectedDateEvents.Clear();
        foreach (var calendarEvent in GetVisibleEventsForDate(SelectedDate))
        {
            SelectedDateEvents.Add(BuildEventItem(calendarEvent));
        }
    }

    private MonthCellViewModel BuildDateCell(SolDate date)
    {
        var visibleEvents = GetVisibleEventsForDate(date).ToArray();
        var eventSlots = visibleEvents
            .Take(3)
            .Select(BuildEventBadge)
            .ToList();

        while (eventSlots.Count < 3)
        {
            eventSlots.Add(EventBadgeViewModel.Placeholder(IsDarkMode));
        }

        var overflowDots = visibleEvents
            .Skip(3)
            .Take(9)
            .Select(BuildOverflowDot)
            .ToArray();

        return new MonthCellViewModel(
            date,
            date.IsSpecialDay ? SolCalendarMath.GetDayName(date) : date.Day.ToString(),
            string.Empty,
            string.Empty,
            date == SelectedDate,
            date == SolCalendarMath.FromGregorian(DateOnly.FromDateTime(_clock.Now.DateTime)),
            eventSlots,
            overflowDots,
            IsDarkMode);
    }

    private EventBadgeViewModel BuildEventBadge(CalendarEvent calendarEvent)
    {
        return new EventBadgeViewModel(
            calendarEvent.Id,
            calendarEvent.Title,
            GetCategoryName(calendarEvent.CategoryId),
            GetCategoryColorHex(calendarEvent.CategoryId),
            IsDarkMode);
    }

    private OverflowDotViewModel BuildOverflowDot(CalendarEvent calendarEvent) =>
        new(GetCategoryColorHex(calendarEvent.CategoryId));

    private EventListItemViewModel BuildEventItem(CalendarEvent calendarEvent)
    {
        var category = _categories.FirstOrDefault(item => string.Equals(item.Id, calendarEvent.CategoryId, StringComparison.OrdinalIgnoreCase));
        return new EventListItemViewModel(
            calendarEvent.Id,
            calendarEvent.Title,
            category?.Name ?? "Uncategorized",
            category?.ColorHex ?? "#475569",
            $"{calendarEvent.Date.LongLabel} · {SolCalendarMath.ToGregorianDate(calendarEvent.Date):MMM d, yyyy}",
            string.IsNullOrWhiteSpace(calendarEvent.Notes) ? "No notes." : calendarEvent.Notes!,
            string.Equals(calendarEvent.Id, _editingEventId, StringComparison.OrdinalIgnoreCase),
            IsDarkMode);
    }

    private IEnumerable<CalendarEvent> GetVisibleEventsForDate(SolDate date)
    {
        return _events
            .Where(evt => evt.Date == date)
            .Where(evt => evt.CategoryId is null || _selectedFilterIds.Contains(evt.CategoryId))
            .OrderBy(evt => GetCategorySortKey(evt.CategoryId), StringComparer.OrdinalIgnoreCase)
            .ThenBy(evt => evt.Title, StringComparer.OrdinalIgnoreCase);
    }

    private void RefreshProgress()
    {
        var now = _clock.Now;
        var currentDate = SolCalendarMath.FromGregorian(DateOnly.FromDateTime(now.DateTime));
        YearProgressValue = SolCalendarMath.GetYearProgress(now);
        MonthProgressValue = SolCalendarMath.GetMonthProgress(now);
        DayProgressValue = SolCalendarMath.GetDayProgress(now);

        YearProgressText = $"{YearProgressValue:P1} through year {now.Year}";
        MonthProgressText = $"{MonthProgressValue:P1} through {(currentDate.IsSpecialDay ? SolCalendarMath.GetDayName(currentDate) : currentDate.MonthName)}";
        DayProgressText = $"{DayProgressValue:P1} through today";
    }

    private const bool IsDarkMode = true;

    private string GetCategorySortKey(string? categoryId) =>
        _categories.FirstOrDefault(category => string.Equals(category.Id, categoryId, StringComparison.OrdinalIgnoreCase))?.Name
        ?? "Uncategorized";

    private string GetCategoryName(string? categoryId) =>
        _categories.FirstOrDefault(category => string.Equals(category.Id, categoryId, StringComparison.OrdinalIgnoreCase))?.Name
        ?? "Uncategorized";

    private string GetCategoryColorHex(string? categoryId) =>
        _categories.FirstOrDefault(category => string.Equals(category.Id, categoryId, StringComparison.OrdinalIgnoreCase))?.ColorHex
        ?? "#475569";

    private async Task RefreshFromDiskAsync(string? forceStatusMessage = null)
    {
        if (_isRefreshingFromDisk)
        {
            return;
        }

        try
        {
            _isRefreshingFromDisk = true;
            var snapshot = await _repository.GetSnapshotAsync();
            var selectedEventId = _editingEventId;

            var hasDataChanged =
                !_categories.SequenceEqual(snapshot.Categories) ||
                !_events.SequenceEqual(snapshot.Events) ||
                !string.Equals(DataFilePath, snapshot.DataFilePath, StringComparison.OrdinalIgnoreCase);

            _categories = snapshot.Categories;
            _events = snapshot.Events;
            DataFilePath = snapshot.DataFilePath;
            _editingEventId = !string.IsNullOrWhiteSpace(selectedEventId) &&
                              _events.Any(evt => string.Equals(evt.Id, selectedEventId, StringComparison.OrdinalIgnoreCase))
                ? selectedEventId
                : null;

            SynchronizeFilterSelection();
            RebuildCategoryFilters();
            RebuildCategoryItems();
            RefreshProgress();
            RefreshSelectionSummaries();
            BuildCalendarViews();

            if (!string.IsNullOrWhiteSpace(forceStatusMessage))
            {
                StatusMessage = forceStatusMessage;
            }
            else if (hasDataChanged)
            {
                StatusMessage = $"Calendar data synced at {_clock.Now:HH:mm:ss}.";
            }
        }
        catch (Exception exception)
        {
            StatusMessage = exception.Message;
        }
        finally
        {
            _isRefreshingFromDisk = false;
        }
    }
}
