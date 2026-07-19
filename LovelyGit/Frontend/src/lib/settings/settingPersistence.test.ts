// @vitest-environment jsdom

import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { sendRequestWithoutResponse } from "@/lib/commands";
import {
	APPEARANCE_PERSIST_DELAY_MS,
	CURRENT_REPOSITORY_PERSIST_DELAY_MS,
	cancelPendingAppearanceSettings,
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

	it("coalesces rapid appearance colors into one bulk persistence", () => {
		for (let index = 0; index < 100; index++) {
			persistSettingValue(
				"LightAccent",
				`#${(0x100000 + index).toString(16).toUpperCase()}`,
			);
		}
		persistSettingValue("LightBackground", "#F8F8F8");
		expect(send).not.toHaveBeenCalled();

		vi.advanceTimersByTime(APPEARANCE_PERSIST_DELAY_MS);

		expect(send).toHaveBeenCalledOnce();
		expect(send).toHaveBeenCalledWith({
			commandType: "SetMultipleSettings",
			arguments: {
				settingValues: {
					LightAccent: "#100063",
					LightBackground: "#F8F8F8",
				},
			},
		});
	});

	it("cancels superseded appearance values before a theme patch", () => {
		persistSettingValue("LightAccent", "#112233");
		persistSettingValue("LightBackground", "#F8F8F8");

		cancelPendingAppearanceSettings(["LightAccent"]);
		vi.advanceTimersByTime(APPEARANCE_PERSIST_DELAY_MS);

		expect(send).toHaveBeenCalledWith({
			commandType: "SetMultipleSettings",
			arguments: { settingValues: { LightBackground: "#F8F8F8" } },
		});
	});

	it("flushes the final repository on pagehide", () => {
		persistSettingValue("CurrentGitRepositoryId", "repo");

		window.dispatchEvent(new Event("pagehide"));

		expect(send).toHaveBeenCalledOnce();
		vi.advanceTimersByTime(CURRENT_REPOSITORY_PERSIST_DELAY_MS);
		expect(send).toHaveBeenCalledOnce();
	});

	it("flushes pending appearance values on pagehide", () => {
		persistSettingValue("DarkForeground", "#F0F0F0");

		window.dispatchEvent(new Event("pagehide"));

		expect(send).toHaveBeenCalledOnce();
		expect(send).toHaveBeenCalledWith({
			commandType: "SetMultipleSettings",
			arguments: { settingValues: { DarkForeground: "#F0F0F0" } },
		});
		vi.advanceTimersByTime(APPEARANCE_PERSIST_DELAY_MS);
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
