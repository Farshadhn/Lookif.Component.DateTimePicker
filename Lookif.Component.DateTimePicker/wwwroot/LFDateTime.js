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


$(document).click(function (e) {

    if (!$(e.target).hasClass("Main-Wrapper-Calendar") && $(e.target).closest(".Main-Wrapper-Calendar").length === 0) {
        $.each(window.LFDatetimes, function (key, value) {
            value.instance.invokeMethodAsync('Toggle');

        });
        window.LFDatetimes = []

    }
});