using DNTPersianUtils.Core;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System.Globalization;

using static System.Globalization.CultureInfo;
namespace Lookif.Component.DateTimePicker;

public partial class LFDateTimePicker : IDisposable
{

    public LFDateTimePicker()
    {
        Identity = Guid.NewGuid();
    }


    [Inject] IJSRuntime _jSRuntime { get; set; }




    private IEnumerable<(int RowNumber, string Name)> MonthNames = DateTimeFormatInfo.CurrentInfo.MonthNames.Select((value, i) => (i, value));
    private IEnumerable<(int RowNumber, string Name)> WeekDays = new List<(int RowNumber, string Name)>()
    {
        (6,"ش"),
        (0,"ی"),
        (1,"د"),
        (2,"س"),
        (3,"چ"),
        (4,"پ"),
        (5,"ج")
    };
    private readonly Guid Identity;
    private int Month => CurrentCulture.Calendar.GetMonth(Now);
    private int Day { get; set; }
    private int Year => CurrentCulture.Calendar.GetYear(Now);
    private TimeOnly Time
    {
        get => _time;
        set
        {
            if (value == _time)
                return;
            _time = value;
            ValueChanged.InvokeAsync(Latest);

        }
    }



    private DotNetObjectReference<LFDateTimePicker> objRef { get; set; }
    private LFDateTimeJSInterop _lFDateTimeJSInterop { get; set; }




    [Parameter]
    public DateTime Value { get; set; } 
    [Parameter]
    public bool CultureType { get; set; } = true;
    public int YearValue { get; set; }
    public int DaysValue { get; set; }
    public int FirstDayOfMonth_DayOfWeek { get; set; }
    public bool Show { get; set; } = false;

    private DateTime Latest => (DateTime.TryParse($"{selectedYear}-{selectedMonth}-{selectedDay} {Time.Hour}:{Time.Minute}", out DateTime d)) ?
              d : DateTime.Parse($"{selectedYear}-{selectedMonth}-1");





    private int selectedYear = CurrentCulture.Calendar.GetYear(Now);
    private int selectedMonth = CurrentCulture.Calendar.GetMonth(Now);
    private int selectedDay = CurrentCulture.Calendar.GetDayOfMonth(Now);
    private TimeOnly _time =  TimeOnly.FromTimeSpan(DateTime.Now.TimeOfDay);

    private int SelectedDay
    {
        get => selectedDay;
        set
        {
            if (value == selectedDay)
                return;
            selectedDay = value;
            ValueChanged.InvokeAsync(Latest);

        }
    }

    private int SelectedMonth
    {
        get => selectedMonth;
        set
        {
            if (value == selectedMonth)
                return;
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
            if (value == selectedYear)
                return;
            selectedYear = value;

            SetFirstDayOfMonth_DayOfWeek();
            SetDaysOfSelectedDateTime();
            ValueChanged.InvokeAsync(Latest);
        }
    }

    private async void EnterTime(KeyboardEventArgs e)
    {
        if (e.Code == "Enter" || e.Code == "NumpadEnter")
        {
            await _lFDateTimeJSInterop.SetOrUnsetInstance(objRef, Identity, false);
            await Toggle();
        }

    }




    private void SetFirstDayOfMonth_DayOfWeek()
    {
        var d = DateTime.Parse($"{SelectedYear}-{SelectedMonth}-{SelectedDay}");
        var persianMonth = d.GetPersianMonthStartAndEndDates();
        FirstDayOfMonth_DayOfWeek = (int)persianMonth.StartDateOnly.DayOfWeek;

    }

    [JSInvokable("Toggle")]
    public async Task Toggle()
    {
        Show = !Show;

        if (Show)
            await _lFDateTimeJSInterop.SetOrUnsetInstance(objRef, Identity, Show);
        StateHasChanged();
    }

    private async Task Day_Click(int value)
    {
        SelectedDay = value;
        await _lFDateTimeJSInterop.SetOrUnsetInstance(objRef, Identity, false);
        await Toggle();
    }
    private void SetDaysOfSelectedDateTime()
    {
        DaysValue = CurrentCulture.Calendar.GetDaysInMonth(SelectedYear, SelectedMonth);
    }


    public void Dispose()
    {
        objRef?.Dispose();
    }
    protected override async Task OnParametersSetAsync()
    {
        YearValue = CurrentCulture.Calendar.GetYear(DateTime.Now);
        SetDaysOfSelectedDateTime();
        SetFirstDayOfMonth_DayOfWeek();
        if (Value != default)
        {

            //SetDaysOfSelectedDateTime();
            //SetFirstDayOfMonth_DayOfWeek();
            var Input = new DateTime(Value.Year, Value.Month, Value.Day);

            var d = CurrentCulture.Calendar.GetDayOfMonth(Input);
            var m = CurrentCulture.Calendar.GetMonth(Input);
            var y = CurrentCulture.Calendar.GetYear(Input);
            Time = TimeOnly.FromDateTime(Value);


            if (SelectedDay != d)
                SelectedDay = d;

            if (SelectedMonth != m)
                SelectedMonth = m;

            if (SelectedYear != y)
                SelectedYear = y;
        }

        MonthNames = DateTimeFormatInfo.CurrentInfo.MonthNames.Select((value, i) => (i, value));
        objRef = DotNetObjectReference.Create(this);
        if (_jSRuntime is not null)
        {
            _lFDateTimeJSInterop = new LFDateTimeJSInterop(_jSRuntime);

        }


    }

    [Parameter]
    public EventCallback<DateTime> ValueChanged { get; set; }

    private static DateTime Now => DateTime.Now;

    public IJSRuntime JSRuntime { get; }

    protected override Task OnInitializedAsync()
    {
        ValueChanged.InvokeAsync(Latest);
        return base.OnInitializedAsync();
    }


}
