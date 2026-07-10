// @vitest-environment jsdom

import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { FileHistoryContextMenu } from "./FileHistoryContextMenu";

describe("FileHistoryContextMenu", () => {
	it("composes directly with a file button and opens history", async () => {
		const user = userEvent.setup();
		const onOpen = vi.fn();
		const onOpenBlame = vi.fn();
		render(
			<FileHistoryContextMenu
				onOpen={onOpen}
				onOpenBlame={onOpenBlame}
				path="src/file.ts"
			>
				<button type="button">src/file.ts</button>
			</FileHistoryContextMenu>,
		);

		await user.pointer({
			keys: "[MouseRight]",
			target: screen.getByRole("button", { name: "src/file.ts" }),
		});
		expect(
			await screen.findByRole("menuitem", { name: "View line blame…" }),
		).toBeVisible();
		await user.click(
			await screen.findByRole("menuitem", { name: "View file history…" }),
		);

		expect(onOpen).toHaveBeenCalledOnce();
	});

	it("offers shared and local ignore actions when supplied", async () => {
		const user = userEvent.setup();
		const onIgnoreLocal = vi.fn();
		const onIgnoreShared = vi.fn();
		render(
			<FileHistoryContextMenu
				onIgnoreLocal={onIgnoreLocal}
				onIgnoreShared={onIgnoreShared}
				onOpen={vi.fn()}
				onOpenBlame={vi.fn()}
				path="build/output.log"
			>
				<button type="button">build/output.log</button>
			</FileHistoryContextMenu>,
		);

		await user.pointer({
			keys: "[MouseRight]",
			target: screen.getByRole("button", { name: "build/output.log" }),
		});
		await user.click(
			await screen.findByRole("menuitem", {
				name: "Add exact path to .gitignore",
			}),
		);

		expect(onIgnoreShared).toHaveBeenCalledOnce();
		expect(onIgnoreLocal).not.toHaveBeenCalled();
	});
});
