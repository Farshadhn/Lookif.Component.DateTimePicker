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

public enum CalendarType
{
    Persian,
    Gregorian
}
public partial class LFDateTimePicker : IDisposable
{
    [Inject] private IJSRuntime _jSRuntime { get; set; }
    [Parameter] public bool Disabled { get; set; }
    [Parameter] public DateTimePickerMode Mode { get; set; } = DateTimePickerMode.DateTime;
    [Parameter] public CalendarType Calendar { get; set; } = CalendarType.Persian;
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

    private bool IsDateOnlyMode => Mode == DateTimePickerMode.DateOnly || 
                                  (DateOnlyValueChanged.HasDelegate && !DateTimeValueChanged.HasDelegate);

    private readonly Guid Identity = Guid.NewGuid();
    private DotNetObjectReference<LFDateTimePicker> objRef;
    private LFDateTimeJSInterop _lFDateTimeJSInterop;
    private bool Show { get; set; }
    private TimeOnly _time = TimeOnly.FromTimeSpan(DateTime.Now.TimeOfDay);

    private int YearValue { get; set; }
    private int DaysValue { get; set; }
    private int FirstDayOfMonth_DayOfWeek { get; set; }

    private Calendar CurrentCalendar => Calendar == CalendarType.Persian 
        ? new PersianCalendar() 
        : new GregorianCalendar();

    private int selectedYear;
    private int selectedMonth;
    private int selectedDay;

    private IEnumerable<(int RowNumber, string Name)> MonthNames
    {
        get
        {
            if (Calendar == CalendarType.Persian)
            {
                return new List<(int, string)>
                {
                    (0, "فروردین"),
                    (1, "اردیبهشت"),
                    (2, "خرداد"),
                    (3, "تیر"),
                    (4, "مرداد"),
                    (5, "شهریور"),
                    (6, "مهر"),
                    (7, "آبان"),
                    (8, "آذر"),
                    (9, "دی"),
                    (10, "بهمن"),
                    (11, "اسفند")
                };
            }
            else
            {
                return DateTimeFormatInfo.GetInstance(new CultureInfo("en-US")).MonthNames
                    .Where(m => !string.IsNullOrEmpty(m))
                    .Select((value, i) => (i, value));
            }
        }
    }

    private IEnumerable<(int RowNumber, string Name)> WeekDays
    {
        get
        {
            if (Calendar == CalendarType.Persian)
            {
                return new List<(int RowNumber, string Name)>
                {
                    (6, "ش"),
                    (0, "ی"),
                    (1, "د"),
                    (2, "س"),
                    (3, "چ"),
                    (4, "پ"),
                    (5, "ج")
                };
            }
            else
            {
                return new List<(int RowNumber, string Name)>
                {
                    (0, "Sun"),
                    (1, "Mon"),
                    (2, "Tue"),
                    (3, "Wed"),
                    (4, "Thu"),
                    (5, "Fri"),
                    (6, "Sat")
                };
            }
        }
    }

    private DateTime Latest
    {
        get
        {
            if (Calendar == CalendarType.Persian)
            {
                if (IsDateOnlyMode)
                {
                    return new DateTime(selectedYear, selectedMonth, selectedDay, CurrentCalendar);
                }
                else
                {
                    var date = new DateTime(selectedYear, selectedMonth, selectedDay, CurrentCalendar);
                    return date.Date.Add(Time.ToTimeSpan());
                }
            }
            else
            {
                if (IsDateOnlyMode)
                {
                    return new DateTime(selectedYear, selectedMonth, selectedDay);
                }
                else
                {
                    if (DateTime.TryParse($"{selectedYear}-{selectedMonth}-{selectedDay} {Time.Hour}:{Time.Minute}", out DateTime d))
                        return d;
                    return new DateTime(selectedYear, selectedMonth, 1);
                }
            }
        }
    }

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

    public void Dispose()
    {
        objRef?.Dispose();
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        
        // Initialize selected date based on calendar type
        var now = DateTime.Now;
        if (Calendar == CalendarType.Persian)
        {
            selectedYear = CurrentCalendar.GetYear(now);
            selectedMonth = CurrentCalendar.GetMonth(now);
            selectedDay = CurrentCalendar.GetDayOfMonth(now);
        }
        else
        {
            selectedYear = now.Year;
            selectedMonth = now.Month;
            selectedDay = now.Day;
        }
        
        NotifyValueChanged();
    }

    protected override async Task OnParametersSetAsync()
    {
        if (Disabled) return;

        var now = DateTime.Now;
        if (Calendar == CalendarType.Persian)
        {
            YearValue = CurrentCalendar.GetYear(now);
        }
        else
        {
            YearValue = now.Year;
        }
        
        SetDaysOfSelectedDateTime();
        SetFirstDayOfMonth_DayOfWeek();

        if (CurrentValue != default)
        {
            DateTime input;
            if (Calendar == CalendarType.Persian)
            {
                // CurrentValue is already in Gregorian, we need to convert it to Persian
                var d = CurrentCalendar.GetDayOfMonth(CurrentValue);
                var m = CurrentCalendar.GetMonth(CurrentValue);
                var y = CurrentCalendar.GetYear(CurrentValue);
                
                if (SelectedDay != d) SelectedDay = d;
                if (SelectedMonth != m) SelectedMonth = m;
                if (SelectedYear != y) SelectedYear = y;
            }
            else
            {
                var d = CurrentValue.Day;
                var m = CurrentValue.Month;
                var y = CurrentValue.Year;
                
                if (SelectedDay != d) SelectedDay = d;
                if (SelectedMonth != m) SelectedMonth = m;
                if (SelectedYear != y) SelectedYear = y;
            }

            // Only set time if we're in DateTime mode
            if (!IsDateOnlyMode)
            {
                Time = TimeOnly.FromDateTime(CurrentValue);
            }
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
        if (Calendar == CalendarType.Persian)
        {
            var firstDayOfMonth = new DateTime(SelectedYear, SelectedMonth, 1, CurrentCalendar);
            FirstDayOfMonth_DayOfWeek = (int)firstDayOfMonth.DayOfWeek;
            // Adjust for Persian week (Saturday = 0)
            FirstDayOfMonth_DayOfWeek = (FirstDayOfMonth_DayOfWeek + 1) % 7;
        }
        else
        {
            var firstDayOfMonth = new DateTime(SelectedYear, SelectedMonth, 1);
            FirstDayOfMonth_DayOfWeek = (int)firstDayOfMonth.DayOfWeek;
            // For Gregorian, Sunday = 0, so no adjustment needed
        }
    }

    private void SetDaysOfSelectedDateTime()
    {
        DaysValue = CurrentCalendar.GetDaysInMonth(SelectedYear, SelectedMonth);
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
        if (Calendar == CalendarType.Persian)
        {
            SelectedYear = CurrentCalendar.GetYear(today);
            SelectedMonth = CurrentCalendar.GetMonth(today);
            SelectedDay = CurrentCalendar.GetDayOfMonth(today);
        }
        else
        {
            SelectedYear = today.Year;
            SelectedMonth = today.Month;
            SelectedDay = today.Day;
        }

        // Only set time if we're in DateTime mode
        if (!IsDateOnlyMode)
        {
            Time = TimeOnly.FromDateTime(today);
        }

        await _lFDateTimeJSInterop.SetOrUnsetInstance(objRef, Identity, false);
        await Toggle();
    }

    private string GetDisplayValue()
    {
        if (Calendar == CalendarType.Persian)
        {
            if (IsDateOnlyMode)
            {
                return $"{selectedYear:0000}/{selectedMonth:00}/{selectedDay:00}";
            }
            else
            {
                return $"{selectedYear:0000}/{selectedMonth:00}/{selectedDay:00} {Time.Hour:00}:{Time.Minute:00}";
            }
        }
        else
        {
            if (IsDateOnlyMode)
            {
                return Latest.ToString("yyyy/MM/dd");
            }
            else
            {
                return Latest.ToString("yyyy/MM/dd HH:mm");
            }
        }
    }

    private string GetHeaderDateText()
    {
        if (Calendar == CalendarType.Persian)
        {
            if (IsDateOnlyMode)
            {
                return DateOnly.FromDateTime(Latest).ToPersianDateTextify();
            }
            else
            {
                return Latest.ToPersianDateTextify();
            }
        }
        else
        {
            if (IsDateOnlyMode)
            {
                return Latest.ToString("dddd, MMMM dd, yyyy", new CultureInfo("en-US"));
            }
            else
            {
                return Latest.ToString("dddd, MMMM dd, yyyy HH:mm", new CultureInfo("en-US"));
            }
        }
    }

    private bool IsHoliday(int dayOfWeek)
    {
        if (Calendar == CalendarType.Persian)
        {
            // Friday in Persian calendar (RowNumber = 5)
            return dayOfWeek == 5;
        }
        else
        {
            // Saturday (6) and Sunday (0) in Gregorian calendar
            return dayOfWeek == 0 || dayOfWeek == 6;
        }
    }
}
