import { describe, expect, it, vi } from "vitest";
import type { KnownGitRepository } from "@/generated/types";
import {
	createPaletteItems,
	filterPaletteItems,
	nextEnabledItem,
} from "./commandPaletteItems";

function create(
	currentRepositoryId: string | null = "repo-1",
	overrides: Partial<Parameters<typeof createPaletteItems>[0]> = {},
) {
	return createPaletteItems({
		currentRepositoryId,
		currentRepositoryPath: currentRepositoryId ? "C:/Example" : null,
		onClose: vi.fn(),
		onCopyRepositoryPath: vi.fn(),
		onCreateBranch: vi.fn(),
		onManageRemotes: vi.fn(),
		onManageStashes: vi.fn(),
		onOpenCommitSearch: vi.fn(),
		onOpenSettings: vi.fn(),
		onOpenRemote: vi.fn(),
		onOpenTerminal: vi.fn(),
		onOpenWorkingChanges: vi.fn(),
		onRefreshRepository: vi.fn(),
		onRevealRepository: vi.fn(),
		repositories: [
			{
				id: "repo-2",
				name: "Example",
				path: "C:/Example",
			} as KnownGitRepository,
		],
		setCurrentRepositoryId: vi.fn().mockResolvedValue(undefined),
		...overrides,
	});
}

describe("command palette items", () => {
	it("disables repository commands without an active repository", () => {
		const items = create(null);
		expect(items.find((item) => item.id === "working")?.disabled).toBe(true);
		expect(items.find((item) => item.id === "terminal")?.disabled).toBe(true);
		expect(
			items.find((item) => item.id === "settings")?.disabled,
		).toBeUndefined();
	});

	it("enables high-frequency repository actions with an active repository", () => {
		const items = create();
		for (const id of [
			"refresh",
			"copy-path",
			"reveal",
			"create-branch",
			"manage-remotes",
			"manage-stashes",
			"terminal",
			"remote-web",
		]) {
			expect(items.find((item) => item.id === id)?.disabled).toBe(false);
		}
	});

	it("keeps path copying disabled until repository metadata is loaded", () => {
		const items = create("repo-1", { currentRepositoryPath: null });
		expect(items.find((item) => item.id === "copy-path")?.disabled).toBe(true);
		expect(items.find((item) => item.id === "reveal")?.disabled).toBe(false);
	});

	it("closes before opening shared repository workflows", () => {
		const order: string[] = [];
		const items = create("repo-1", {
			onClose: () => order.push("close"),
			onCreateBranch: () => order.push("create"),
		});
		items.find((item) => item.id === "create-branch")?.run();
		expect(order).toEqual(["close", "create"]);
	});

	it("matches labels, descriptions, and keywords across terms", () => {
		expect(
			filterPaletteItems(create(), "stage working").map((item) => item.id),
		).toEqual(["working"]);
		expect(
			filterPaletteItems(create(), "example").map((item) => item.id),
		).toEqual(["repo:repo-2"]);
	});

	it("wraps keyboard navigation and skips disabled commands", () => {
		const items = create(null);
		const settingsIndex = items.findIndex((item) => item.id === "settings");
		expect(nextEnabledItem(items, items.length - 1, 1)).toBe(settingsIndex);
		expect(nextEnabledItem(items, settingsIndex, -1)).toBe(items.length - 1);
	});
});
