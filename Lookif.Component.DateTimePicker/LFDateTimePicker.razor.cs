using DNTPersianUtils.Core;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System.Globalization;

using static System.Globalization.CultureInfo;
namespace Lookif.Component.DateTimePicker;

public partial class LFDateTimePicker : IDisposable
{
    [Inject] private IJSRuntime _jSRuntime { get; set; }
    [Parameter] public bool Disabled { get; set; }
    [Parameter] public DateTime Value { get; set; }
    [Parameter] public bool CultureType { get; set; } = true;
    [Parameter] public EventCallback<DateTime> ValueChanged { get; set; }

    private readonly Guid Identity = Guid.NewGuid();
    private DotNetObjectReference<LFDateTimePicker> objRef;
    private LFDateTimeJSInterop _lFDateTimeJSInterop;
    private bool Show { get; set; }
    private TimeOnly _time = TimeOnly.FromTimeSpan(DateTime.Now.TimeOfDay);

    private int YearValue { get; set; }
    private int DaysValue { get; set; }
    private int FirstDayOfMonth_DayOfWeek { get; set; }

    private int selectedYear = CurrentCulture.Calendar.GetYear(Now);
    private int selectedMonth = CurrentCulture.Calendar.GetMonth(Now);
    private int selectedDay = CurrentCulture.Calendar.GetDayOfMonth(Now);

    private IEnumerable<(int RowNumber, string Name)> MonthNames => 
        DateTimeFormatInfo.CurrentInfo.MonthNames.Select((value, i) => (i, value));

    private IEnumerable<(int RowNumber, string Name)> WeekDays => new List<(int RowNumber, string Name)>
    {
        (6, "ش"),
        (0, "ی"),
        (1, "د"),
        (2, "س"),
        (3, "چ"),
        (4, "پ"),
        (5, "ج")
    };

    private DateTime Latest => DateTime.TryParse($"{selectedYear}-{selectedMonth}-{selectedDay} {Time.Hour}:{Time.Minute}", out DateTime d)
        ? d
        : DateTime.Parse($"{selectedYear}-{selectedMonth}-1");

    private TimeOnly Time
    {
        get => _time;
        set
        {
            if (value == _time) return;
            _time = value;
            ValueChanged.InvokeAsync(Latest);
        }
    }

    private int SelectedDay
    {
        get => selectedDay;
        set
        {
            if (value == selectedDay) return;
            selectedDay = value;
            ValueChanged.InvokeAsync(Latest);
        }
    }

    private int SelectedMonth
    {
        get => selectedMonth;
        set
        {
            if (value == selectedMonth) return;
            selectedMonth = value;
            SetFirstDayOfMonth_DayOfWeek();
            SetDaysOfSelectedDateTime();
            ValueChanged.InvokeAsync(Latest);
        }
    }

    private int SelectedYear
    {
        get => selectedYear;
        set
        {
            if (value == selectedYear) return;
            selectedYear = value;
            SetFirstDayOfMonth_DayOfWeek();
            SetDaysOfSelectedDateTime();
            ValueChanged.InvokeAsync(Latest);
        }
    }

    private static DateTime Now => DateTime.Now;
    private static CultureInfo CurrentCulture => CultureInfo.CurrentCulture;

    public void Dispose()
    {
        objRef?.Dispose();
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        ValueChanged.InvokeAsync(Latest);
    }

    protected override async Task OnParametersSetAsync()
    {
        if (Disabled) return;

        YearValue = CurrentCulture.Calendar.GetYear(DateTime.Now);
        SetDaysOfSelectedDateTime();
        SetFirstDayOfMonth_DayOfWeek();

        if (Value != default)
        {
            var input = new DateTime(Value.Year, Value.Month, Value.Day);
            var d = CurrentCulture.Calendar.GetDayOfMonth(input);
            var m = CurrentCulture.Calendar.GetMonth(input);
            var y = CurrentCulture.Calendar.GetYear(input);
            Time = TimeOnly.FromDateTime(Value);

            if (SelectedDay != d) SelectedDay = d;
            if (SelectedMonth != m) SelectedMonth = m;
            if (SelectedYear != y) SelectedYear = y;
        }

        objRef = DotNetObjectReference.Create(this);
        if (_jSRuntime is not null)
        {
            _lFDateTimeJSInterop = new LFDateTimeJSInterop(_jSRuntime);
        }
    }

    private void SetFirstDayOfMonth_DayOfWeek()
    {
        var d = DateTime.Parse($"{SelectedYear}-{SelectedMonth}-{SelectedDay}");
        var persianMonth = d.GetPersianMonthStartAndEndDates();
        FirstDayOfMonth_DayOfWeek = (int)persianMonth.StartDateOnly.DayOfWeek;
    }

    private void SetDaysOfSelectedDateTime()
    {
        DaysValue = CurrentCulture.Calendar.GetDaysInMonth(SelectedYear, SelectedMonth);
    }

    [JSInvokable("Toggle")]
    public async Task Toggle()
    {
        if (Disabled) return;
        Show = !Show;

        if (Show)
        {
            await _lFDateTimeJSInterop.SetOrUnsetInstance(objRef, Identity, Show);
        }
        StateHasChanged();
    }

    private async Task Day_Click(int value)
    {
        SelectedDay = value;
        await _lFDateTimeJSInterop.SetOrUnsetInstance(objRef, Identity, false);
        await Toggle();
    }

    private async void EnterTime(KeyboardEventArgs e)
    {
        if (e.Code == "Enter" || e.Code == "NumpadEnter")
        {
            await _lFDateTimeJSInterop.SetOrUnsetInstance(objRef, Identity, false);
            await Toggle();
        }
    }

    private async Task SetToToday()
    {
        var today = DateTime.Now;
        SelectedYear = CurrentCulture.Calendar.GetYear(today);
        SelectedMonth = CurrentCulture.Calendar.GetMonth(today);
        SelectedDay = CurrentCulture.Calendar.GetDayOfMonth(today);
        Time = TimeOnly.FromDateTime(today);
        
        await _lFDateTimeJSInterop.SetOrUnsetInstance(objRef, Identity, false);
        await Toggle();
    }
}
