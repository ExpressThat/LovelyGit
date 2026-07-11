// @vitest-environment jsdom

import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import type { HeadCommitMessageResponse } from "@/generated/types";
import { UndoLastCommitDialog } from "./UndoLastCommitDialog";

describe("UndoLastCommitDialog", () => {
	it("explains the soft reset, previews the commit, and confirms", async () => {
		const user = userEvent.setup();
		const onConfirm = vi.fn();
		renderDialog({ onConfirm, preview: head() });

		expect(screen.getByText("Original title")).toBeVisible();
		expect(screen.getByText("Original body")).toBeVisible();
		expect(screen.getByText(/files stay staged/i)).toBeVisible();
		expect(screen.getByText(/local branch will diverge/i)).toBeVisible();
		await user.click(screen.getByRole("button", { name: "Undo commit" }));
		expect(onConfirm).toHaveBeenCalledOnce();
	});

	it("describes merge first-parent behavior", () => {
		renderDialog({ preview: head({ parentCount: 2 }) });
		expect(screen.getByText(/merge commit.*first parent/i)).toBeVisible();
	});

	it("disables undo for the initial commit", () => {
		renderDialog({ preview: head({ firstParentHash: null, parentCount: 0 }) });
		expect(screen.getByText(/initial commit has no parent/i)).toBeVisible();
		expect(screen.getByRole("button", { name: "Undo commit" })).toBeDisabled();
	});

	it("keeps cancel and confirmation disabled while mutating", () => {
		renderDialog({ isUndoing: true, preview: head() });
		expect(screen.getByRole("button", { name: "Cancel" })).toBeDisabled();
		expect(
			screen.getByRole("button", { name: "Undoing commit…" }),
		).toBeDisabled();
	});

	it("surfaces a retryable failure without losing the preview", () => {
		renderDialog({ error: "index is locked", preview: head() });
		expect(screen.getByText("index is locked")).toBeVisible();
		expect(screen.getByText("Original title")).toBeVisible();
		expect(screen.getByRole("button", { name: "Undo commit" })).toBeEnabled();
	});
});

function renderDialog(
	overrides: Partial<Parameters<typeof UndoLastCommitDialog>[0]> = {},
) {
	render(
		<UndoLastCommitDialog
			error={null}
			isLoading={false}
			isOpen
			isUndoing={false}
			onClose={vi.fn()}
			onConfirm={vi.fn()}
			preview={null}
			{...overrides}
		/>,
	);
}

function head(
	overrides: Partial<HeadCommitMessageResponse> = {},
): HeadCommitMessageResponse {
	return {
		body: "Original body",
		firstParentHash: "b".repeat(40),
		hash: "a".repeat(40),
		parentCount: 1,
		title: "Original title",
		...overrides,
	};
}
