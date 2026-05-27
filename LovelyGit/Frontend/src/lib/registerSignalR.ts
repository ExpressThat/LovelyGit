import { type HubConnection, HubConnectionBuilder } from "@microsoft/signalr"
import { nanoid } from 'nanoid'

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


export const CommsHubCommandType = {
    KnownGitRepositorys: "KnownGitRepositorys",
    CommitGraph: "CommitGraph",
    Settings: "Settings"
}

export const CommsHubSubCommandType = {
    Get: "Get",
    Create: "Create",
    Update: "Update",
    Delete: "Delete"
}

export type CommandResponse<T> =
    {
        commandUniqueId?: string,
        commandType: typeof CommsHubCommandType[keyof typeof CommsHubCommandType]
        subCommandType?: typeof CommsHubSubCommandType[keyof typeof CommsHubSubCommandType]
        isSuccess: boolean
        errorMessage?: string,
        result?: T
    }

export type CommsHubCommand = {
    commandUniqueId?: string;
    commandType: typeof CommsHubCommandType[keyof typeof CommsHubCommandType]
    subCommandType?: typeof CommsHubSubCommandType[keyof typeof CommsHubSubCommandType]
    Key?: string,
    Arguments?: Record<string, string | undefined | null>
}


export async function sendRequestWithResponse<T>(commandInput: Omit<CommsHubCommand, "commandUniqueId">) {
    const sr = getSignalR();
    const commandId = nanoid();

    const promise = new Promise<CommandResponse<T>>((resolve) => {
        const handleResult = (result: CommandResponse<T>) => {
            if (result.commandUniqueId === commandId) {
                sr.off("Result", handleResult);
                resolve(result)
            }
        };

        sr.on("Result", handleResult);
    });

    const startTime = performance.now()
    await sendRequestWithoutResponse({
        ...commandInput,
        commandUniqueId: commandId
    });

    const response = await promise;
    const endTime = performance.now()

    console.log(`Call to sendRequestWithResponse took ${endTime - startTime} milliseconds`)
    if (!response.isSuccess) {
        throw new Error(response.errorMessage ?? "Command failed");
    }

    return response.result;
}

export async function sendRequestWithoutResponse(commandInput: CommsHubCommand) {
    await getSignalR().invoke("Command", commandInput);
}
