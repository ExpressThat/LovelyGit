// @vitest-environment jsdom

import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { FileHistoryContextMenu } from "./FileHistoryContextMenu";

describe("FileHistoryContextMenu", () => {
	it("composes directly with a file button and opens history", async () => {
		const user = userEvent.setup();
		const onOpen = vi.fn();
		render(
			<FileHistoryContextMenu onOpen={onOpen} path="src/file.ts">
				<button type="button">src/file.ts</button>
			</FileHistoryContextMenu>,
		);

		await user.pointer({
			keys: "[MouseRight]",
			target: screen.getByRole("button", { name: "src/file.ts" }),
		});
		await user.click(
			await screen.findByRole("menuitem", { name: "View file history…" }),
		);

		expect(onOpen).toHaveBeenCalledOnce();
	});
});
