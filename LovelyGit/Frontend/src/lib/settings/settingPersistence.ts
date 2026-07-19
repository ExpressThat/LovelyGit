import { sendRequestWithoutResponse } from "@/lib/commands";
import type { Settings, SettingsKey } from "./Settings";

export const CURRENT_REPOSITORY_PERSIST_DELAY_MS = 500;
export const APPEARANCE_PERSIST_DELAY_MS = 100;

const coalescedAppearanceKeys = new Set<SettingsKey>([
	"DarkAccent",
	"DarkBackground",
	"DarkForeground",
	"LightAccent",
	"LightBackground",
	"LightForeground",
]);

let pendingRepositoryId: string | null | undefined;
let repositoryTimer: number | null = null;
let pendingAppearanceValues: Record<string, unknown> = {};
let appearanceTimer: number | null = null;

export function persistSettingValue<K extends SettingsKey>(
	key: K,
	value: Settings[K],
) {
	if (key !== "CurrentGitRepositoryId") {
		if (coalescedAppearanceKeys.has(key)) {
			queueAppearanceSetting(key, value);
		} else {
			sendSetting(key, value);
		}
		return;
	}

	pendingRepositoryId = value as Settings["CurrentGitRepositoryId"];
	if (repositoryTimer != null) window.clearTimeout(repositoryTimer);
	repositoryTimer = window.setTimeout(
		flushPendingRepositorySetting,
		CURRENT_REPOSITORY_PERSIST_DELAY_MS,
	);
}

export function flushPendingAppearanceSettings() {
	if (appearanceTimer != null) window.clearTimeout(appearanceTimer);
	appearanceTimer = null;
	if (Object.keys(pendingAppearanceValues).length === 0) return;
	const settingValues = pendingAppearanceValues;
	pendingAppearanceValues = {};
	sendRequestWithoutResponse({
		commandType: "SetMultipleSettings",
		arguments: { settingValues },
	});
}

export function cancelPendingAppearanceSettings(keys?: Iterable<SettingsKey>) {
	if (keys) {
		for (const key of keys) delete pendingAppearanceValues[key];
	} else {
		pendingAppearanceValues = {};
	}

	if (Object.keys(pendingAppearanceValues).length > 0) return;
	if (appearanceTimer != null) window.clearTimeout(appearanceTimer);
	appearanceTimer = null;
}

export function flushPendingRepositorySetting() {
	if (repositoryTimer != null) window.clearTimeout(repositoryTimer);
	repositoryTimer = null;
	if (pendingRepositoryId === undefined) return;
	const value = pendingRepositoryId;
	pendingRepositoryId = undefined;
	sendSetting("CurrentGitRepositoryId", value);
}

export function cancelPendingRepositorySetting() {
	if (repositoryTimer != null) window.clearTimeout(repositoryTimer);
	repositoryTimer = null;
	pendingRepositoryId = undefined;
}

export function resetSettingPersistenceForTests() {
	cancelPendingRepositorySetting();
	cancelPendingAppearanceSettings();
}

function queueAppearanceSetting<K extends SettingsKey>(
	key: K,
	value: Settings[K],
) {
	pendingAppearanceValues[key] = value;
	if (appearanceTimer != null) window.clearTimeout(appearanceTimer);
	appearanceTimer = window.setTimeout(
		flushPendingAppearanceSettings,
		APPEARANCE_PERSIST_DELAY_MS,
	);
}

function sendSetting<K extends SettingsKey>(key: K, value: Settings[K]) {
	sendRequestWithoutResponse({
		commandType: "SetSetting",
		arguments: { setting: key, value },
	});
}

if (typeof window !== "undefined") {
	window.addEventListener("pagehide", () => {
		flushPendingRepositorySetting();
		flushPendingAppearanceSettings();
	});
}
