"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("/notificationHub").build();

connection.on("ReceiveNotification", function (message) {
    toastr.info(message);
});

connection.start().catch(function (err) {
    return console.error(err.toString());
});
