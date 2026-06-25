import {
	NativeMessageType,
	NativeMessageTypesWithRequest,
	NativeMessageTypesWithResponse,
	type NativeMessageRequest,
	type NativeMessageResponse,
	type NativeMessageTypesWithRequest as NativeMessageTypesWithRequestValue,
	type NativeMessageTypesWithRequestAndResponse,
	type NativeMessageTypesWithRequestWithoutResponse,
	type NativeMessageTypesWithResponse as NativeMessageTypesWithResponseValue,
	type NativeMessageType as NativeMessageTypeValue,
	type NativeRequestBodies,
	type NativeResponseBodies,
	type NativeSubscriber,
} from "./types";
import {
	hasNativeMessaging,
	registerNativeMessageHandlers,
	sendNativeMessage,
} from "./transport";

type PendingRequest = {
	resolve: (body: unknown) => void;
	reject: (error: Error) => void;
	timeoutId: number;
};

const defaultTimeoutMs = 10_000;
const pendingRequests = new Map<string, PendingRequest>();
const requestMessageTypes = new Set<NativeMessageTypeValue>(
	NativeMessageTypesWithRequest,
);
const responseMessageTypes = new Set<NativeMessageTypeValue>(
	NativeMessageTypesWithResponse,
);
const subscribers = new Map<
	NativeMessageTypesWithResponseValue,
	Set<NativeSubscriber<NativeMessageTypesWithResponseValue>>
>();
let messageHandlersRegistered = false;

export function requestNativeMessage<
	TMessageType extends NativeMessageTypesWithRequestAndResponse,
>(
	messageType: TMessageType,
	body: NativeRequestBodies[TMessageType],
	timeoutMs?: number,
): Promise<NativeResponseBodies[TMessageType]>;
export function requestNativeMessage<
	TMessageType extends NativeMessageTypesWithRequestWithoutResponse,
>(
	messageType: TMessageType,
	body: NativeRequestBodies[TMessageType],
	timeoutMs?: number,
): void;
export function requestNativeMessage<
	TMessageType extends NativeMessageTypesWithRequestValue,
>(
	messageType: TMessageType,
	body: NativeRequestBodies[TMessageType],
	timeoutMs = defaultTimeoutMs,
) {
	ensureMessageHandlersRegistered();
	if (!requestMessageTypes.has(messageType)) {
		throw new Error(`'${messageType}' cannot be sent from the frontend.`);
	}

	const expectsResponse = responseMessageTypes.has(messageType);

	if (!hasNativeMessaging()) {
		const error = new Error("Native messaging is unavailable.");
		if (expectsResponse) {
			return Promise.reject(error);
		}

		throw error;
	}

	const messageId = crypto.randomUUID();
	const request: NativeMessageRequest<TMessageType> = {
		messageId,
		body,
	};

	if (!expectsResponse) {
		sendNativeMessage(messageType, request);
		return;
	}

	return new Promise<any>((resolve, reject) => {
		const timeoutId = window.setTimeout(() => {
			pendingRequests.delete(getPendingKey(messageType, messageId));
			reject(new Error(`Timed out waiting for '${messageType}' response.`));
		}, timeoutMs);

		pendingRequests.set(getPendingKey(messageType, messageId), {
			resolve: resolve as (body: unknown) => void,
			reject,
			timeoutId,
		});

		sendNativeMessage(messageType, request);
	});
}

export function subscribeNativeMessage<
	TMessageType extends NativeMessageTypesWithResponseValue,
>(
	messageType: TMessageType,
	callback: NativeSubscriber<TMessageType>,
) {
	ensureMessageHandlersRegistered();

	const typedSubscribers = getSubscribers(messageType);
	typedSubscribers.add(callback as NativeSubscriber<NativeMessageTypesWithResponseValue>);

	return () => {
		typedSubscribers.delete(callback as NativeSubscriber<NativeMessageTypesWithResponseValue>);
	};
}

function ensureMessageHandlersRegistered() {
	if (messageHandlersRegistered) {
		return;
	}

	messageHandlersRegistered = registerNativeMessageHandlers(handleNativeResponse);
}

function handleNativeResponse<TMessageType extends NativeMessageTypeValue>(
	messageType: TMessageType,
	payload?: string,
) {
	if (!responseMessageTypes.has(messageType)) {
		return;
	}

	const responseMessageType = messageType as NativeMessageTypesWithResponseValue;
	const response = parseResponse(responseMessageType, payload);
	if (!response) {
		return;
	}

	const pendingKey = getPendingKey(messageType, response.messageId);
	const pendingRequest = pendingRequests.get(pendingKey);
	if (pendingRequest) {
		window.clearTimeout(pendingRequest.timeoutId);
		pendingRequests.delete(pendingKey);

		if (response.success) {
			pendingRequest.resolve(response.body);
		} else {
			pendingRequest.reject(new Error(response.error ?? "Native message failed."));
		}
	}

	for (const subscriber of getSubscribers(responseMessageType)) {
		subscriber(response as NativeMessageResponse<NativeMessageTypesWithResponseValue>);
	}
}

function parseResponse<TMessageType extends NativeMessageTypesWithResponseValue>(
	_messageType: TMessageType,
	payload?: string,
) {
	if (payload === undefined) {
		return null;
	}

	try {
		const response = JSON.parse(payload) as Partial<NativeMessageResponse<TMessageType>>;
		if (
			typeof response.messageId !== "string" ||
			typeof response.success !== "boolean"
		) {
			return null;
		}

		return response as NativeMessageResponse<TMessageType>;
	} catch {
		return null;
	}
}

function getSubscribers(messageType: NativeMessageTypesWithResponseValue) {
	let typedSubscribers = subscribers.get(messageType);
	if (!typedSubscribers) {
		typedSubscribers = new Set();
		subscribers.set(messageType, typedSubscribers);
	}

	return typedSubscribers;
}

function getPendingKey(messageType: NativeMessageTypeValue, messageId: string) {
	return `${messageType}:${messageId}`;
}

export { hasNativeMessaging, NativeMessageType };
