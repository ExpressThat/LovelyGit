// @vitest-environment jsdom

import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { Search } from "@/components/icons/lovelyIcons";
import { CommandPaletteList } from "./CommandPaletteList";
import type { PaletteItem } from "./commandPaletteItems";
import { isCommandPaletteShortcut } from "./commandPaletteShortcut";

describe("isCommandPaletteShortcut", () => {
	it("accepts Ctrl or Meta K without conflicting modifiers", () => {
		expect(
			isCommandPaletteShortcut(
				new KeyboardEvent("keydown", { ctrlKey: true, key: "k" }),
			),
		).toBe(true);
		expect(
			isCommandPaletteShortcut(
				new KeyboardEvent("keydown", { key: "K", metaKey: true }),
			),
		).toBe(true);
		expect(
			isCommandPaletteShortcut(
				new KeyboardEvent("keydown", { altKey: true, ctrlKey: true, key: "k" }),
			),
		).toBe(false);
	});
});

describe("CommandPaletteList", () => {
	it("bounds a large collection and runs visible items", () => {
		const items = paletteItems(100);
		const view = render(
			<CommandPaletteList
				activeIndex={0}
				items={items}
				onActiveIndexChange={vi.fn()}
			/>,
		);

		expect(
			view.container.querySelector("[data-command-palette-list='virtual']"),
		).toBeInTheDocument();
		expect(
			view.container.querySelectorAll("[data-command-palette-row]").length,
		).toBeLessThanOrEqual(10);
		fireEvent.click(screen.getByRole("button", { name: /Command 0/ }));
		expect(items[0]?.run).toHaveBeenCalledOnce();
	});

	it("retains ordinary rows and mounts a distant active virtual item", () => {
		const view = render(
			<CommandPaletteList
				activeIndex={0}
				items={paletteItems(12)}
				onActiveIndexChange={vi.fn()}
			/>,
		);
		expect(
			view.container.querySelectorAll("[data-command-palette-row]"),
		).toHaveLength(12);

		view.rerender(
			<CommandPaletteList
				activeIndex={99}
				items={paletteItems(100)}
				onActiveIndexChange={vi.fn()}
			/>,
		);
		expect(screen.getByRole("button", { name: /Command 99/ })).toBeVisible();
	});
});

function paletteItems(count: number): PaletteItem[] {
	return Array.from({ length: count }, (_, index) => ({
		description: `Description ${index}`,
		icon: Search,
		id: `${index}`,
		keywords: "command",
		label: `Command ${index}`,
		run: vi.fn(),
	}));
}
