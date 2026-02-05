const connection = new signalR.HubConnectionBuilder()
    .withUrl("/chathub")
    .withAutomaticReconnect()
    .build();

connection.start();

connection.on("RefreshMessages", () => {
    location.reload();
});
