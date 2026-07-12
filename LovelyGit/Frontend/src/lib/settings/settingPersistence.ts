import { sendRequestWithoutResponse } from "@/lib/commands";
import type { Settings, SettingsKey } from "./Settings";

export const CURRENT_REPOSITORY_PERSIST_DELAY_MS = 500;

let pendingRepositoryId: string | null | undefined;
let repositoryTimer: number | null = null;

export function persistSettingValue<K extends SettingsKey>(
	key: K,
	value: Settings[K],
) {
	if (key !== "CurrentGitRepositoryId") {
		sendSetting(key, value);
		return;
	}

	pendingRepositoryId = value as Settings["CurrentGitRepositoryId"];
	if (repositoryTimer != null) window.clearTimeout(repositoryTimer);
	repositoryTimer = window.setTimeout(
		flushPendingRepositorySetting,
		CURRENT_REPOSITORY_PERSIST_DELAY_MS,
	);
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
}

function sendSetting<K extends SettingsKey>(key: K, value: Settings[K]) {
	sendRequestWithoutResponse({
		commandType: "SetSetting",
		arguments: { setting: key, value },
	});
}

if (typeof window !== "undefined") {
	window.addEventListener("pagehide", flushPendingRepositorySetting);
}
