// @vitest-environment jsdom

import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { sendRequestWithoutResponse } from "@/lib/commands";
import {
	CURRENT_REPOSITORY_PERSIST_DELAY_MS,
	cancelPendingRepositorySetting,
	flushPendingRepositorySetting,
	persistSettingValue,
	resetSettingPersistenceForTests,
} from "./settingPersistence";

vi.mock("@/lib/commands", () => ({ sendRequestWithoutResponse: vi.fn() }));

const send = vi.mocked(sendRequestWithoutResponse);

describe("settingPersistence", () => {
	beforeEach(() => {
		vi.useFakeTimers();
		resetSettingPersistenceForTests();
		send.mockReset();
	});

	afterEach(() => vi.useRealTimers());

	it("coalesces rapid repository selections", () => {
		persistSettingValue("CurrentGitRepositoryId", "repo-a");
		persistSettingValue("CurrentGitRepositoryId", "repo-b");
		expect(send).not.toHaveBeenCalled();

		vi.advanceTimersByTime(CURRENT_REPOSITORY_PERSIST_DELAY_MS);

		expect(send).toHaveBeenCalledOnce();
		expect(send).toHaveBeenCalledWith({
			commandType: "SetSetting",
			arguments: {
				setting: "CurrentGitRepositoryId",
				value: "repo-b",
			},
		});
	});

	it("persists other settings immediately", () => {
		persistSettingValue("CommitDiffWrapLines", true);

		expect(send).toHaveBeenCalledOnce();
	});

	it("flushes the final repository on pagehide", () => {
		persistSettingValue("CurrentGitRepositoryId", "repo");

		window.dispatchEvent(new Event("pagehide"));

		expect(send).toHaveBeenCalledOnce();
		vi.advanceTimersByTime(CURRENT_REPOSITORY_PERSIST_DELAY_MS);
		expect(send).toHaveBeenCalledOnce();
	});

	it("can cancel or explicitly flush pending persistence", () => {
		persistSettingValue("CurrentGitRepositoryId", "cancelled");
		cancelPendingRepositorySetting();
		vi.advanceTimersByTime(CURRENT_REPOSITORY_PERSIST_DELAY_MS);
		expect(send).not.toHaveBeenCalled();

		persistSettingValue("CurrentGitRepositoryId", null);
		flushPendingRepositorySetting();
		expect(send).toHaveBeenCalledWith({
			commandType: "SetSetting",
			arguments: { setting: "CurrentGitRepositoryId", value: null },
		});
	});
});
