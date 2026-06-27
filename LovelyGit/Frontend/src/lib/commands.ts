import {
	NativeMessageType,
	type NativeMessageTypesWithRequest,
	type NativeMessageTypesWithResponseWithoutRequest,
	type NativeRequestBodies,
	type NativeResponseBodies,
	requestNativeMessage,
	subscribeNativeMessage,
} from "@/lib/nativeMessaging";

type CommandInput<TCommand extends NativeMessageTypesWithRequest> = {
	commandType: TCommand;
	arguments?: NativeRequestBodies[TCommand];
};

type SetSettingInput = {
	commandType: typeof NativeMessageType.SetSetting;
	arguments: {
		setting: NativeRequestBodies[typeof NativeMessageType.SetSetting]["setting"];
		value: unknown;
	};
};

type SetMultipleSettingsInput = {
	commandType: typeof NativeMessageType.SetMultipleSettings;
	arguments: {
		settingValues: Record<string, unknown>;
	};
};

type NativeRequestSender = (
	commandType: NativeMessageTypesWithRequest,
	body: NativeRequestBodies[NativeMessageTypesWithRequest],
) => Promise<unknown> | undefined;

export async function sendRequestWithResponse<
	TCommand extends NativeMessageTypesWithRequest,
>(
	command: CommandInput<TCommand> | SetSettingInput | SetMultipleSettingsInput,
): Promise<
	TCommand extends keyof NativeResponseBodies
		? NativeResponseBodies[TCommand]
		: undefined
> {
	const sendNativeRequest = requestNativeMessage as NativeRequestSender;
	const requestResult = sendNativeRequest(
		command.commandType,
		toNativeBody(command) as NativeRequestBodies[TCommand],
	);
	const response =
		requestResult instanceof Promise ? await requestResult : undefined;
	return response as TCommand extends keyof NativeResponseBodies
		? NativeResponseBodies[TCommand]
		: undefined;
}

export function sendRequestWithoutResponse<
	TCommand extends NativeMessageTypesWithRequest,
>(
	command: CommandInput<TCommand> | SetSettingInput | SetMultipleSettingsInput,
) {
	const sendNativeRequest = requestNativeMessage as NativeRequestSender;
	sendNativeRequest(
		command.commandType,
		toNativeBody(command) as NativeRequestBodies[TCommand],
	);
}

export function subscribeToServerEvent<
	TEvent extends NativeMessageTypesWithResponseWithoutRequest,
>(
	eventName: TEvent,
	listener: (notification: NativeResponseBodies[TEvent]) => void,
) {
	return subscribeNativeMessage(eventName, (response) => {
		if (response.success && response.body !== null) {
			listener(response.body as NativeResponseBodies[TEvent]);
		}
	});
}

function toNativeBody<TCommand extends NativeMessageTypesWithRequest>(
	command: CommandInput<TCommand> | SetSettingInput | SetMultipleSettingsInput,
) {
	if (isSetSettingInput(command)) {
		return {
			setting: command.arguments.setting,
			valueJson: JSON.stringify(command.arguments.value),
		};
	}

	if (isSetMultipleSettingsInput(command)) {
		return {
			settingValueJsons: Object.fromEntries(
				Object.entries(command.arguments.settingValues).map(
					([setting, value]) => [setting, JSON.stringify(value)],
				),
			),
		};
	}

	return command.arguments ?? {};
}

function isSetSettingInput(command: unknown): command is SetSettingInput {
	return (
		typeof command === "object" &&
		command !== null &&
		"commandType" in command &&
		command.commandType === NativeMessageType.SetSetting
	);
}

function isSetMultipleSettingsInput(
	command: unknown,
): command is SetMultipleSettingsInput {
	return (
		typeof command === "object" &&
		command !== null &&
		"commandType" in command &&
		command.commandType === NativeMessageType.SetMultipleSettings
	);
}
