export function SetOrUnsetInstance(dotNetHelper, identity, IsItSet) {

    if (window.LFDatetimes == undefined)
        window.LFDatetimes = [];
    var obj = { "instance": dotNetHelper, "identity": identity };

    if (IsItSet)
        window.LFDatetimes.push(obj);
    else
        removeItemOnce(window.LFDatetimes, identity);
}

function removeItemOnce(arr, identity) {
    window.LFDatetimes = window.LFDatetimes.filter(function (item) { return item.identity != identity })
}

// Position the popup below the input
export function lfdt_setPopupPosition(inputId, popupId) {
    var input = document.getElementById(inputId);
    var popup = document.getElementById(popupId);
    if (input && popup) {
        var rect = input.getBoundingClientRect();
        popup.style.left = rect.left + 'px';
        popup.style.top = (rect.bottom + window.scrollY) + 'px';
        popup.style.width = rect.width + 'px'; // Optional: match input width
 
    }
}

$(document).click(function (e) {

    if (!$(e.target).hasClass("Main-Wrapper-Calendar") && $(e.target).closest(".Main-Wrapper-Calendar").length === 0) {
        $.each(window.LFDatetimes, function (key, value) {
            value.instance.invokeMethodAsync('Toggle');

        });
        window.LFDatetimes = []

    }
});