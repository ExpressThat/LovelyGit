// @vitest-environment jsdom

import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { PatchApplyDialog } from "./PatchApplyDialog";

describe("PatchApplyDialog", () => {
	it("previews files and returns the selected options", async () => {
		const user = userEvent.setup();
		const onApply = vi.fn();
		const onReverseChange = vi.fn();
		const onStageChangesChange = vi.fn();
		render(
			<PatchApplyDialog
				isApplying={false}
				onApply={onApply}
				onOpenChange={vi.fn()}
				onReverseChange={onReverseChange}
				onStageChangesChange={onStageChangesChange}
				preview={preview}
				reverse={false}
				stageChanges={false}
			/>,
		);

		await waitFor(() => expect(screen.getByText("src/file.ts")).toBeVisible());
		expect(screen.getAllByText("+2")).toHaveLength(2);
		await user.click(screen.getByLabelText("Stage applied changes"));
		await user.click(screen.getByLabelText("Reverse patch"));
		await user.click(screen.getByRole("button", { name: "Apply patch" }));

		expect(onStageChangesChange).toHaveBeenCalledWith(true);
		expect(onReverseChange).toHaveBeenCalledWith(true);
		expect(onApply).toHaveBeenCalledOnce();
	});

	it("locks every action while Git is applying", async () => {
		render(
			<PatchApplyDialog
				isApplying
				onApply={vi.fn()}
				onOpenChange={vi.fn()}
				onReverseChange={vi.fn()}
				onStageChangesChange={vi.fn()}
				preview={preview}
				reverse={false}
				stageChanges={false}
			/>,
		);

		expect(screen.getByRole("button", { name: "Cancel" })).toBeDisabled();
		expect(
			screen.getByRole("button", { name: "Checking and applying…" }),
		).toBeDisabled();
		expect(
			await screen.findByLabelText("Stage applied changes"),
		).toHaveAttribute("aria-disabled", "true");
		expect(screen.getByLabelText("Reverse patch")).toHaveAttribute(
			"aria-disabled",
			"true",
		);
	});
});

const preview = {
	fileName: "change.patch",
	files: [{ additions: 2, deletions: 1, path: "src/file.ts" }],
	isTruncated: false,
	path: "C:/patches/change.patch",
	selected: true,
	totalAdditions: 2,
	totalDeletions: 1,
};
