// @vitest-environment jsdom

import { fireEvent, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { TagContextMenu } from "./TagContextMenu";

describe("TagContextMenu", () => {
	it("creates a branch at the tagged commit", async () => {
		const user = userEvent.setup();
		const onCreateBranch = vi.fn();
		render(
			<TagContextMenu
				commitHash="1111111111111111111111111111111111111111"
				disabled={false}
				onAction={vi.fn()}
				onCreateBranch={onCreateBranch}
				remoteName="origin"
				tagName="v2.0.0"
			>
				<button type="button">v2.0.0 tag</button>
			</TagContextMenu>,
		);

		fireEvent.contextMenu(screen.getByRole("button", { name: "v2.0.0 tag" }));
		await user.click(await screen.findByText("Create branch from v2.0.0…"));

		expect(onCreateBranch).toHaveBeenCalledWith(
			"v2.0.0",
			"1111111111111111111111111111111111111111",
		);
	});
});
