import { type HubConnection, HubConnectionBuilder } from "@microsoft/signalr";
import { nanoid } from "nanoid";
import type {
	CommandResponse,
	CommsHubCommand,
} from "@/generated/ExpressThat.LovelyGit.Services.Hubs.Commands";
import type {
	ResponseForCommand,
	TypedCommsHubCommandInput,
	TypedSetSettingsCommandInput,
} from "@/generated/LovelyGit.CommandContracts";

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

export async function sendRequestWithResponse<
	TCommand extends TypedCommsHubCommandInput,
>(commandInput: TCommand): Promise<ResponseForCommand<TCommand> | undefined> {
	const sr = getSignalR();
	const commandId = nanoid();

	const promise = new Promise<CommandResponse<ResponseForCommand<TCommand>>>(
		(resolve) => {
			const handleResult = (
				result: CommandResponse<ResponseForCommand<TCommand>>,
			) => {
				if (result.commandUniqueId === commandId) {
					sr.off("Result", handleResult);
					resolve(result);
				}
			};

			sr.on("Result", handleResult);
		},
	);

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
	TCommand extends TypedCommsHubCommandInput,
>(commandInput: TCommand) {
	await invokeCommand(toWireCommand(commandInput));
}

async function invokeCommand(commandInput: CommsHubCommand<unknown>) {
	await getSignalR().invoke("Command", commandInput);
}

function toWireCommand(
	commandInput: TypedCommsHubCommandInput,
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
	commandInput: TypedCommsHubCommandInput,
): commandInput is TypedSetSettingsCommandInput {
	return (
		commandInput.commandType === "Settings" &&
		commandInput.subCommandType === "Set"
	);
}
