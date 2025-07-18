"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("/notificationHub").build();

connection.on("ReceiveNotification", function (message) {
    toastr.info(message);
});

connection.start().catch(function (err) {
    return console.error(err.toString());
});
(function () {
    // TempData toastr popups
    $(function () {
        var success = $('#TempDataSuccess').val();
        var error = $('#TempDataError').val();

        if (success) { toastr.success(success); }
        if (error) { toastr.error(error); }
    });

    //SignalR notifications
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/notificationHub")
        .build();

    connection.on("ReceiveNotification", function (message) {
        toastr.info(message);
    });

    connection.start().catch(function (err) {
        return console.error(err.toString());
    });
})();
