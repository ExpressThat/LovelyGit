// @vitest-environment jsdom

import { beforeEach, describe, expect, it, vi } from "vitest";
import type { NativeMessageType as NativeMessageTypeValue } from "./types";

vi.mock("./performance", () => ({
	recordNativeMessagePerformance: vi.fn(),
}));
vi.mock("./transport", () => ({
	hasNativeMessaging: vi.fn(),
	registerNativeMessageHandlers: vi.fn(),
	sendNativeMessage: vi.fn(),
}));

describe("native messaging client", () => {
	beforeEach(() => {
		vi.resetModules();
		vi.clearAllMocks();
	});

	it("correlates a successful response and records its duration", async () => {
		const harness = await createHarness();
		const response = harness.client.requestNativeMessage(
			harness.types.NativeMessageType.GetAllSettings,
			{},
		);
		const request = harness.sentRequest();

		harness.receive(
			harness.types.NativeMessageType.GetAllSettings,
			JSON.stringify({
				body: { Theme: "Dark" },
				messageId: request.messageId,
				success: true,
			}),
		);

		await expect(response).resolves.toEqual({ Theme: "Dark" });
		expect(
			harness.performance.recordNativeMessagePerformance,
		).toHaveBeenCalledWith(
			harness.types.NativeMessageType.GetAllSettings,
			expect.objectContaining({ messageId: request.messageId, success: true }),
			expect.any(Number),
		);
	});

	it("rejects a correlated native failure", async () => {
		const harness = await createHarness();
		const response = harness.client.requestNativeMessage(
			harness.types.NativeMessageType.GetAllSettings,
			{},
		);
		const request = harness.sentRequest();

		harness.receive(
			harness.types.NativeMessageType.GetAllSettings,
			JSON.stringify({
				error: "settings unavailable",
				messageId: request.messageId,
				success: false,
			}),
		);

		await expect(response).rejects.toThrow("settings unavailable");
	});

	it("times out requests and ignores their late responses", async () => {
		vi.useFakeTimers();
		try {
			const harness = await createHarness();
			const response = harness.client.requestNativeMessage(
				harness.types.NativeMessageType.GetAllSettings,
				{},
				25,
			);
			const request = harness.sentRequest();
			const rejection = expect(response).rejects.toThrow("Timed out waiting");

			await vi.advanceTimersByTimeAsync(25);
			await rejection;
			harness.receive(
				harness.types.NativeMessageType.GetAllSettings,
				JSON.stringify({
					body: {},
					messageId: request.messageId,
					success: true,
				}),
			);
			expect(
				harness.performance.recordNativeMessagePerformance,
			).not.toHaveBeenCalled();
		} finally {
			vi.useRealTimers();
		}
	});

	it("sends fire-and-forget commands without creating a promise", async () => {
		const harness = await createHarness();
		const result = harness.client.requestNativeMessage(
			harness.types.NativeMessageType.SetSetting,
			{ setting: null, valueJson: null },
		);

		expect(result).toBeUndefined();
		expect(harness.transport.sendNativeMessage).toHaveBeenCalledOnce();
	});

	it("rejects or throws when the native host is unavailable", async () => {
		const harness = await createHarness(false);

		await expect(
			harness.client.requestNativeMessage(
				harness.types.NativeMessageType.GetAllSettings,
				{},
			),
		).rejects.toThrow("Native messaging is unavailable");
		expect(() =>
			harness.client.requestNativeMessage(
				harness.types.NativeMessageType.SetSetting,
				{ setting: null, valueJson: null },
			),
		).toThrow("Native messaging is unavailable");
	});

	it("delivers valid notifications until unsubscribed", async () => {
		const harness = await createHarness();
		const subscriber = vi.fn();
		const unsubscribe = harness.client.subscribeNativeMessage(
			harness.types.NativeMessageType.WorkingTreeChanged,
			subscriber,
		);
		const response = JSON.stringify({
			body: { repositoryId: "repo" },
			messageId: "event",
			success: true,
		});

		harness.receive(harness.types.NativeMessageType.WorkingTreeChanged, "bad");
		harness.receive(
			harness.types.NativeMessageType.WorkingTreeChanged,
			response,
		);
		unsubscribe();
		harness.receive(
			harness.types.NativeMessageType.WorkingTreeChanged,
			response,
		);

		expect(subscriber).toHaveBeenCalledOnce();
		expect(subscriber).toHaveBeenCalledWith(
			expect.objectContaining({ messageId: "event", success: true }),
		);
	});
});

async function createHarness(nativeAvailable = true) {
	const transport = await import("./transport");
	const performance = await import("./performance");
	const types = await import("./types");
	let receiver:
		| ((messageType: NativeMessageTypeValue, payload?: string) => void)
		| undefined;
	vi.mocked(transport.hasNativeMessaging).mockReturnValue(nativeAvailable);
	vi.mocked(transport.registerNativeMessageHandlers).mockImplementation(
		(callback) => {
			receiver = callback;
			return true;
		},
	);
	const client = await import("./client");
	return {
		client,
		performance,
		receive: (messageType: NativeMessageTypeValue, payload?: string) =>
			receiver?.(messageType, payload),
		sentRequest: () =>
			vi.mocked(transport.sendNativeMessage).mock.calls.at(-1)?.[1] as {
				messageId: string;
			},
		transport,
		types,
	};
}
