import { describe, expect, it, vi } from "vitest";

const calls = vi.hoisted(() => [] as string[]);

vi.mock("./lib/settings/settingsStore", async () => {
	const { DEFAULT_SETTINGS } = await vi.importActual<
		typeof import("./lib/settings/Settings")
	>("./lib/settings/Settings");

	return {
		initSettingsStore: vi.fn(async () => {
			calls.push("settings");
		}),
		getSetting: vi.fn((key: keyof typeof DEFAULT_SETTINGS) => {
			return DEFAULT_SETTINGS[key];
		}),
	};
});

vi.mock("react-dom/client", () => ({
	default: {
		createRoot: vi.fn(() => ({
			render: vi.fn(() => {
				calls.push("render");
			}),
		})),
	},
}));

vi.mock("./lib/settings/theme/themeUtils", () => ({
	applyThemeToDocument: vi.fn(),
	calculateAppearanceSide: vi.fn(() => "light"),
	calculateTheme: vi.fn(() => "Morning"),
}));

vi.mock("./lib/settings/font/fontUtils", () => ({
	applyFontsToDocument: vi.fn(),
}));

vi.mock("./App", () => ({
	default: () => null,
}));

vi.mock("./components/ui/tooltip", () => ({
	TooltipProvider: ({ children }: { children: React.ReactNode }) => children,
}));

describe("bootstrapApp", () => {
	it("initializes settings before rendering the app", async () => {
		calls.length = 0;
		const { bootstrapApp } = await import("./bootstrapApp");

		await bootstrapApp({} as HTMLElement);

		expect(calls).toEqual(["settings", "render"]);
	});
});
