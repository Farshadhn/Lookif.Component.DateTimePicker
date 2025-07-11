# LFDateTimePicker Component

A Blazor component for Persian date and time picking that supports both nullable `DateTime` and `DateOnly` types with a clean, type-safe API.

## Features

- Persian calendar support
- Both DateTime and DateOnly modes (automatically detected)
- Time picker (only in DateTime mode)
- Responsive design
- Disabled state support
- Today button functionality
- Type-safe nullable parameters

## Usage

### Nullable DateTime

```razor
<LFDateTimePicker @bind-DateTimeValue="selectedDateTime" />

@code {
    private DateTime? selectedDateTime = DateTime.Now;
}
```

### Nullable DateOnly

```razor
<LFDateTimePicker @bind-DateOnlyValue="selectedDate" />

@code {
    private DateOnly? selectedDate = DateOnly.FromDateTime(DateTime.Now);
}
```

### Disabled State

```razor
<LFDateTimePicker @bind-DateTimeValue="selectedDateTime" Disabled="true" />
```

## Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `DateTimeValue` | `DateTime?` | The nullable DateTime value (use with `@bind-DateTimeValue`) |
| `DateOnlyValue` | `DateOnly?` | The nullable DateOnly value (use with `@bind-DateOnlyValue`) |
| `DateTimeValueChanged` | `EventCallback<DateTime?>` | Event callback for DateTime changes |
| `DateOnlyValueChanged` | `EventCallback<DateOnly?>` | Event callback for DateOnly changes |
| `Disabled` | `bool` | Whether the component is disabled (default: false) |

## Behavior

- **DateTime Mode**: Shows both date and time picker. The input displays the full date and time.
- **DateOnly Mode**: Shows only the date picker. The input displays only the date (without time).
- The component automatically detects which mode to use based on which parameter is provided.
- When in DateOnly mode, the time picker is hidden and time-related functionality is disabled.
- All parameters are nullable, allowing for unselected states.

## Dependencies

- `DNTPersianUtils.Core` - For Persian date utilities
- `Microsoft.AspNetCore.Components` - For Blazor components
- `Microsoft.JSInterop` - For JavaScript interop

## Demo

Visit `/demo` to see examples of both nullable DateTime and DateOnly modes in action. 