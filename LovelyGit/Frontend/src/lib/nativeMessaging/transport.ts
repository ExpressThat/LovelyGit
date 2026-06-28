import {
	NativeMessageType,
	type NativeMessageType as NativeMessageTypeValue,
} from "./types";

type NativeMessageHandler = (payload?: string) => void;

type InfiniFrameMessaging = {
	sendMessageToHost(id: string, data?: unknown): void;
	assignMessageReceivedHandler(
		messageId: string,
		callback: NativeMessageHandler,
	): void;
};

declare global {
	interface Window {
		infiniframe?: {
			messaging?: InfiniFrameMessaging;
		};
	}
}

export function hasNativeMessaging() {
	return typeof window.infiniframe?.messaging?.sendMessageToHost === "function";
}

export function sendNativeMessage(
	messageType: NativeMessageTypeValue,
	request: unknown,
) {
	window.infiniframe?.messaging?.sendMessageToHost(messageType, request);
}

export function registerNativeMessageHandlers(
	callback: (messageType: NativeMessageTypeValue, payload?: string) => void,
) {
	const messaging = window.infiniframe?.messaging;
	if (typeof messaging?.assignMessageReceivedHandler !== "function") {
		return false;
	}

	for (const messageType of Object.values(NativeMessageType)) {
		messaging.assignMessageReceivedHandler(messageType, (payload) => {
			callback(messageType, payload);
		});
	}

	return true;
}
