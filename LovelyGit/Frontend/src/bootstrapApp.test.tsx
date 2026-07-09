import { describe, expect, it, vi } from "vitest";

const calls = vi.hoisted(() => [] as string[]);

vi.mock("./lib/settings/settingsStore", () => ({
	getSetting: vi.fn(() => ""),
	initSettingsStore: vi.fn(async () => {
		calls.push("settings");
	}),
}));

vi.mock("./lib/settings/theme/themeUtils", () => ({
	applyThemeToDocument: vi.fn(),
	calculateAppearanceSide: vi.fn(() => "light"),
	calculateTheme: vi.fn(() => ({})),
}));

vi.mock("./lib/settings/font/fontUtils", () => ({
	applyFontsToDocument: vi.fn(),
}));

vi.mock("react-dom/client", () => ({
	default: {
		createRoot: vi.fn(() => ({
			render: vi.fn(() => {
				calls.push("render");
			}),
		})),
	},
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
