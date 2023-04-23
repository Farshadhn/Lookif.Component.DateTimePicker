using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Lookif.Component.DateTimePicker
{
    // This class provides an example of how JavaScript functionality can be wrapped
    // in a .NET class for easy consumption. The associated JavaScript module is
    // loaded on demand when first needed.
    //
    // This class can be registered as scoped DI service and then injected into Blazor
    // components for use.

    public class LFDateTimeJSInterop : IAsyncDisposable
    {
        private readonly Lazy<Task<IJSObjectReference>> moduleTask;

        public LFDateTimeJSInterop(IJSRuntime jsRuntime)
        {
            moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
                "import", "./_content/Lookif.Component.DateTimePicker/LFDateTime.js").AsTask());
        }
        public async ValueTask SetOrUnsetInstance(DotNetObjectReference<LFDateTimePicker> dotNetObjectReference,Guid identity,bool IsItSet)
        {
            var module = await moduleTask.Value;
            await module.InvokeVoidAsync("SetOrUnsetInstance", dotNetObjectReference, identity, IsItSet);
        }
     
        public async ValueTask DisposeAsync()
        {
            if (moduleTask.IsValueCreated)
            {
                var module = await moduleTask.Value;
                await module.DisposeAsync();
            }
        }
    }
}