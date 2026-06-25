import { useRef, useSyncExternalStore } from "react";
import { sendRequestWithResponse, sendRequestWithoutResponse } from "@/lib/commands";
import { DEFAULT_SETTINGS, type Settings, type SettingsKey } from "./Settings";

type Listener = () => void;
type KeyedListeners = { [K in SettingsKey]: Set<Listener> };

let settings: Settings = { ...DEFAULT_SETTINGS };
let initialized = false;
let initPromise: Promise<void> | null = null;

const allListeners = new Set<Listener>();
const keyedListeners = createKeyedListeners();

function createKeyedListeners(): KeyedListeners {
	const entries = Object.keys(DEFAULT_SETTINGS).map((key) => [
		key,
		new Set<Listener>(),
	]);
	return Object.fromEntries(entries) as KeyedListeners;
}

function cloneSettings(): Settings {
	return { ...settings };
}

function notify(changedKeys: SettingsKey[]) {
	for (const key of changedKeys) {
		for (const listener of keyedListeners[key]) {
			listener();
		}
	}

	if (changedKeys.length > 0) {
		for (const listener of allListeners) {
			listener();
		}
	}
}

export async function initSettingsStore(): Promise<void> {
	if (initialized) {
		return;
	}

	if (initPromise) {
		return initPromise;
	}

	initPromise = (async () => {
		const remoteSettings =
			((await sendRequestWithResponse({ commandType: "GetAllSettings" })) as
				| Partial<Settings>
				| undefined) ?? {};
		settings = {
			...DEFAULT_SETTINGS,
			...remoteSettings,
		};
		initialized = true;
		notify(Object.keys(DEFAULT_SETTINGS) as SettingsKey[]);
	})()
		.catch((error) => {
			settings = { ...DEFAULT_SETTINGS };
			initialized = true;
			console.error(
				"Failed to initialize settings store. Falling back to defaults.",
				error,
			);
			notify(Object.keys(DEFAULT_SETTINGS) as SettingsKey[]);
		})
		.finally(() => {
			initPromise = null;
		});

	return initPromise;
}

export function getSetting<K extends SettingsKey>(key: K): Settings[K] {
	return settings[key];
}

export function subscribeToAll(listener: Listener): () => void {
	allListeners.add(listener);
	return () => {
		allListeners.delete(listener);
	};
}

export function subscribeToKey<K extends SettingsKey>(
	key: K,
	listener: Listener,
): () => void {
	keyedListeners[key].add(listener);
	return () => {
		keyedListeners[key].delete(listener);
	};
}

export async function setSetting<K extends SettingsKey>(
	key: K,
	value: Settings[K],
): Promise<void> {
	await initSettingsStore();
	if (Object.is(settings[key], value)) {
		return;
	}

	settings = { ...settings, [key]: value };
	notify([key]);

	try {
		sendRequestWithoutResponse({
			commandType: "SetSetting",
			arguments: {
				setting: key,
				value,
			},
		});
	} catch (error) {
		console.error(`Failed to persist setting "${String(key)}".`, error);
	}
}

export async function setSettings(patch: Partial<Settings>): Promise<void> {
	await initSettingsStore();
	const changedEntries: Array<[SettingsKey, Settings[SettingsKey]]> = [];

	for (const key of Object.keys(patch) as SettingsKey[]) {
		const nextValue = patch[key];
		if (nextValue === undefined || Object.is(settings[key], nextValue)) {
			continue;
		}
		changedEntries.push([key, nextValue]);
	}

	if (changedEntries.length === 0) {
		return;
	}

	const updates = Object.fromEntries(changedEntries) as Partial<Settings>;
	settings = { ...settings, ...updates };
	notify(changedEntries.map(([key]) => key));

	try {
		sendRequestWithoutResponse({
			commandType: "SetMultipleSettings",
			arguments: {
				settingValues: updates,
			},
		});
	} catch (error) {
		console.error("Failed to persist settings patch.", error);
	}
}

export function useSetting<K extends SettingsKey>(key: K): Settings[K] {
	return useSyncExternalStore(
		(onStoreChange) => subscribeToKey(key, onStoreChange),
		() => settings[key],
		() => settings[key],
	);
}

export function useAllSettings(): Settings {
	return useSyncExternalStore(subscribeToAll, cloneSettings, cloneSettings);
}

export function useSettings<T>(
	selector: (state: Settings) => T,
	isEqual: (a: T, b: T) => boolean = Object.is,
): T {
	const selectedRef = useRef<T>(selector(settings));
	return useSyncExternalStore(
		subscribeToAll,
		() => {
			const next = selector(settings);
			if (!isEqual(selectedRef.current, next)) {
				selectedRef.current = next;
			}
			return selectedRef.current;
		},
		() => selectedRef.current,
	);
}
