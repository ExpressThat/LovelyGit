import { describe, expect, it, vi } from "vitest";
import type { KnownGitRepository } from "@/generated/types";
import {
	createPaletteItems,
	filterPaletteItems,
	nextEnabledItem,
} from "./commandPaletteItems";

function create(currentRepositoryId: string | null = "repo-1") {
	return createPaletteItems({
		currentRepositoryId,
		onClose: vi.fn(),
		onOpenCommitSearch: vi.fn(),
		onOpenSettings: vi.fn(),
		onOpenRemote: vi.fn(),
		onOpenTerminal: vi.fn(),
		onOpenWorkingChanges: vi.fn(),
		onRefreshRepository: vi.fn(),
		repositories: [
			{
				id: "repo-2",
				name: "Example",
				path: "C:/Example",
			} as KnownGitRepository,
		],
		setCurrentRepositoryId: vi.fn().mockResolvedValue(undefined),
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
		for (const id of ["refresh", "terminal", "remote-web"]) {
			expect(items.find((item) => item.id === id)?.disabled).toBe(false);
		}
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
