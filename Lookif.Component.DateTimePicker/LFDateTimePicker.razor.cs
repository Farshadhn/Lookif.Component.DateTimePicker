using DNTPersianUtils.Core;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System.Globalization;

using static System.Globalization.CultureInfo;
namespace Lookif.Component.DateTimePicker;
public enum DateTimePickerMode
{

    DateTime,
    DateOnly
}
public partial class LFDateTimePicker : IDisposable
{
    [Inject] private IJSRuntime _jSRuntime { get; set; }
    [Parameter] public bool Disabled { get; set; }
    [Parameter] public DateTimePickerMode Mode { get; set; } = DateTimePickerMode.DateTime;
    // Parameters for different types
    [Parameter] public DateTime DateTimeValue { get; set; }
    [Parameter] public DateOnly DateOnlyValue { get; set; }
    [Parameter] public EventCallback<DateTime> DateTimeValueChanged { get; set; }
    [Parameter] public EventCallback<DateOnly> DateOnlyValueChanged { get; set; }

    // Helper properties to determine the type and get the value
    private DateTime CurrentValue
    {
        get
        {
            if (!IsDateOnlyMode) return DateTimeValue;
            else return DateOnlyValue.ToDateTime(TimeOnly.MinValue);

        }
    }

    private bool IsDateOnlyMode => Mode == DateTimePickerMode.DateOnly;

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
            NotifyValueChanged();
        }
    }

    private int SelectedDay
    {
        get => selectedDay;
        set
        {
            if (value == selectedDay) return;
            selectedDay = value;
            NotifyValueChanged();
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
            NotifyValueChanged();
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
            NotifyValueChanged();
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
        NotifyValueChanged();
    }

    protected override async Task OnParametersSetAsync()
    {
        if (Disabled) return;

        YearValue = CurrentCulture.Calendar.GetYear(DateTime.Now);
        SetDaysOfSelectedDateTime();
        SetFirstDayOfMonth_DayOfWeek();

        if (CurrentValue != default)
        {
            var input = new DateTime(CurrentValue.Year, CurrentValue.Month, CurrentValue.Day);
            var d = CurrentCulture.Calendar.GetDayOfMonth(input);
            var m = CurrentCulture.Calendar.GetMonth(input);
            var y = CurrentCulture.Calendar.GetYear(input);

            // Only set time if we're in DateTime mode
            if (!IsDateOnlyMode)
            {
                Time = TimeOnly.FromDateTime(CurrentValue);
            }

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

    private void NotifyValueChanged()
    {
        if (IsDateOnlyMode)
        {
            var dateOnly = DateOnly.FromDateTime(Latest);
            DateOnlyValueChanged.InvokeAsync(dateOnly);
        }
        else
        {
            DateTimeValueChanged.InvokeAsync(Latest);
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
            Show = !Show;
            await _lFDateTimeJSInterop.SetOrUnsetInstance(objRef, Identity, true);
            // Position the popup below the input using JSInterop wrapper
            if (_lFDateTimeJSInterop != null)
            {
                
                await _lFDateTimeJSInterop.SetPopupPosition($"lfdt-input-{Identity}", $"lfdt-popup-{Identity}");
               
            }
            Show = !Show;
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

        // Only set time if we're in DateTime mode
        if (!IsDateOnlyMode)
        {
            Time = TimeOnly.FromDateTime(today);
        }

        await _lFDateTimeJSInterop.SetOrUnsetInstance(objRef, Identity, false);
        await Toggle();
    }
}
