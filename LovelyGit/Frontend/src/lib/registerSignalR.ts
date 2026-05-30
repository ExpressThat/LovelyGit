import type { CommandResponse, CommsHubCommand, EmptyCommandArguments } from "@/generated/ExpressThat.LovelyGit.Services.Hubs.Commands";
import { type HubConnection, HubConnectionBuilder } from "@microsoft/signalr";
import { nanoid } from "nanoid";

export function getSignalR() {
	return (
		window as unknown as {
			SignalRConCommsHub: HubConnection;
		}
	).SignalRConCommsHub;
}

export async function registerSignalR() {
	var connection = new HubConnectionBuilder()
		.withUrl("/commsHub")
		.withAutomaticReconnect()
		.build();

	(
		window as unknown as {
			SignalRConCommsHub: HubConnection;
		}
	).SignalRConCommsHub = connection;

	await connection.start();
	console.log("done");
}

export async function sendRequestWithResponse<TResponse, TArguments = EmptyCommandArguments>(
	commandInput: Omit<CommsHubCommand<TArguments>, "commandUniqueId">,
) {
	const sr = getSignalR();
	const commandId = nanoid();

	const promise = new Promise<CommandResponse<TResponse>>((resolve) => {
		const handleResult = (result: CommandResponse<TResponse>) => {
			if (result.commandUniqueId === commandId) {
				sr.off("Result", handleResult);
				resolve(result);
			}
		};

		sr.on("Result", handleResult);
	});

	const startTime = performance.now();
	await sendRequestWithoutResponse({
		...commandInput,
		commandUniqueId: commandId,
	});

	const response = await promise;
	const endTime = performance.now();

	console.log(
		`Call to sendRequestWithResponse took ${endTime - startTime} milliseconds`,
	);
	if (!response.isSuccess) {
		throw new Error(response.errorMessage ?? "Command failed");
	}

	return response.result;
}

export async function sendRequestWithoutResponse<TArguments = EmptyCommandArguments>(
	commandInput: CommsHubCommand<TArguments>,
) {
	await getSignalR().invoke("Command", commandInput);
}
