// @vitest-environment jsdom

import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import {
	sendRequestWithoutResponse,
	sendRequestWithResponse,
} from "@/lib/commands";

vi.mock("@/lib/commands", () => ({
	sendRequestWithoutResponse: vi.fn(),
	sendRequestWithResponse: vi.fn(),
}));

const send = vi.mocked(sendRequestWithoutResponse);
const request = vi.mocked(sendRequestWithResponse);

describe("settingsStore persistence", () => {
	beforeEach(() => {
		vi.useFakeTimers();
		vi.resetModules();
		send.mockReset();
		request.mockReset();
		request.mockResolvedValue({});
	});

	afterEach(() => vi.useRealTimers());

	it("prevents a pending color drag from overriding a selected theme", async () => {
		const store = await import("./settingsStore");
		await store.initSettingsStore();
		await store.setSetting("LightAccent", "#112233");
		expect(send).not.toHaveBeenCalled();

		await store.setSettings({ LightAccent: "", LightTheme: "Morning" });
		expect(send).toHaveBeenCalledOnce();
		expect(send).toHaveBeenCalledWith({
			commandType: "SetMultipleSettings",
			arguments: { settingValues: { LightAccent: "" } },
		});

		vi.runAllTimers();
		expect(send).toHaveBeenCalledOnce();
	});
});
