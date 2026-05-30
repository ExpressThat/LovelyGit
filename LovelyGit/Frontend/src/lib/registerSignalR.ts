import { type HubConnection, HubConnectionBuilder } from "@microsoft/signalr";
import { nanoid } from "nanoid";
import type {
	CommandResponse,
	CommsHubCommand,
	EmptyCommandArguments,
} from "@/generated/ExpressThat.LovelyGit.Services.Hubs.Commands";
import type {
	SettingValueMap,
	TypedCommsHubCommandInput,
	TypedGetSettingsCommandInput,
	TypedSetSettingsCommandInput,
} from "@/generated/LovelyGit.SettingContracts";

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

export function sendRequestWithResponse<TSetting extends keyof SettingValueMap>(
	commandInput: TypedGetSettingsCommandInput<TSetting>,
): Promise<SettingValueMap[TSetting] | undefined>;
export function sendRequestWithResponse<
	TResponse,
	TArguments = EmptyCommandArguments,
>(
	commandInput: TypedCommsHubCommandInput<TArguments>,
): Promise<TResponse | undefined>;
export async function sendRequestWithResponse<
	TResponse,
	TArguments = EmptyCommandArguments,
>(commandInput: TypedCommsHubCommandInput<TArguments>) {
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
	await invokeCommand({
		...toWireCommand(commandInput),
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

export async function sendRequestWithoutResponse<
	TArguments = EmptyCommandArguments,
>(commandInput: TypedCommsHubCommandInput<TArguments>) {
	await invokeCommand(toWireCommand(commandInput));
}

async function invokeCommand(commandInput: CommsHubCommand<unknown>) {
	await getSignalR().invoke("Command", commandInput);
}

function toWireCommand<TArguments>(
	commandInput: TypedCommsHubCommandInput<TArguments>,
): CommsHubCommand<unknown> {
	if (isSettingsSetCommand(commandInput)) {
		return {
			...commandInput,
			arguments: {
				setting: commandInput.arguments.setting,
				valueJson: JSON.stringify(commandInput.arguments.value),
			},
		};
	}

	return commandInput;
}

function isSettingsSetCommand(
	commandInput: TypedCommsHubCommandInput<unknown>,
): commandInput is TypedSetSettingsCommandInput {
	return (
		commandInput.commandType === "Settings" &&
		commandInput.subCommandType === "Set"
	);
}
