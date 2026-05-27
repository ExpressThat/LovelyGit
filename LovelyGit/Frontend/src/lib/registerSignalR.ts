import { type HubConnection, HubConnectionBuilder } from "@microsoft/signalr"


export function getSignalR() {
    return (window as unknown as {
        SignalRConCommsHub: HubConnection
    }).SignalRConCommsHub;
}


export async function registerSignalR() {
    var connection = new HubConnectionBuilder()
        .withUrl("/commsHub")
        .withAutomaticReconnect()
        .build();

    (window as unknown as {
        SignalRConCommsHub: HubConnection
    }).SignalRConCommsHub = connection;

    await connection.start();
    console.log("done");
}