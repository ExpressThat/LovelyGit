// @vitest-environment jsdom

import { beforeEach, describe, expect, it, vi } from "vitest";
import {
	hasNativeMessaging,
	registerNativeMessageHandlers,
	sendNativeMessage,
} from "./transport";
import { NativeMessageType } from "./types";

describe("native messaging transport", () => {
	beforeEach(() => {
		delete window.infiniframe;
	});

	it("reports availability only when the host sender exists", () => {
		expect(hasNativeMessaging()).toBe(false);
		window.infiniframe = {
			messaging: {
				assignMessageReceivedHandler: vi.fn(),
				sendMessageToHost: vi.fn(),
			},
		};
		expect(hasNativeMessaging()).toBe(true);
	});

	it("passes messages to the host without reshaping them", () => {
		const sendMessageToHost = vi.fn();
		window.infiniframe = {
			messaging: {
				assignMessageReceivedHandler: vi.fn(),
				sendMessageToHost,
			},
		};
		const request = { body: { repositoryId: "repo" }, messageId: "id" };

		sendNativeMessage(NativeMessageType.FetchRepository, request);

		expect(sendMessageToHost).toHaveBeenCalledWith(
			NativeMessageType.FetchRepository,
			request,
		);
	});

	it("registers every native message and retains its message type", () => {
		const handlers = new Map<string, (payload?: string) => void>();
		window.infiniframe = {
			messaging: {
				assignMessageReceivedHandler: (type, handler) =>
					handlers.set(type, handler),
				sendMessageToHost: vi.fn(),
			},
		};
		const callback = vi.fn();

		expect(registerNativeMessageHandlers(callback)).toBe(true);
		expect(handlers.size).toBe(Object.values(NativeMessageType).length);
		handlers.get(NativeMessageType.WorkingTreeChanged)?.("payload");
		expect(callback).toHaveBeenCalledWith(
			NativeMessageType.WorkingTreeChanged,
			"payload",
		);
	});

	it("returns false when the host cannot register handlers", () => {
		expect(registerNativeMessageHandlers(vi.fn())).toBe(false);
	});
});
