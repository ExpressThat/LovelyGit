// @vitest-environment jsdom

import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { DeleteRemoteTagDialog } from "./DeleteRemoteTagDialog";

describe("DeleteRemoteTagDialog", () => {
	it("explains remote scope and confirms deletion", async () => {
		const user = userEvent.setup();
		const onConfirm = vi.fn();
		render(
			<DeleteRemoteTagDialog
				isBusy={false}
				onConfirm={onConfirm}
				onOpenChange={vi.fn()}
				remoteName="origin"
				tagName="v2.0.0"
			/>,
		);

		expect(
			screen.getByRole("heading", { name: "Delete v2.0.0 from origin?" }),
		).toBeVisible();
		expect(screen.getByText(/Your local tag remains available/)).toBeVisible();
		await user.click(screen.getByRole("button", { name: "Delete remote tag" }));
		expect(onConfirm).toHaveBeenCalledOnce();
	});

	it("cannot dismiss or confirm while deletion is running", () => {
		render(
			<DeleteRemoteTagDialog
				isBusy
				onConfirm={vi.fn()}
				onOpenChange={vi.fn()}
				remoteName="origin"
				tagName="v2.0.0"
			/>,
		);

		expect(
			screen.getByRole("button", { name: "Keep remote tag" }),
		).toBeDisabled();
		expect(screen.getByRole("button", { name: "Deleting" })).toBeDisabled();
	});
});
