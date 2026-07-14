// @vitest-environment jsdom

import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { type RepositoryStashItem, StashAction } from "@/generated/types";
import { DropStashDialog } from "./DropStashDialog";

const target: RepositoryStashItem = {
	commitHash: "0123456789abcdef",
	createdAtUnixSeconds: 1_700_000_000,
	message: "checkpoint",
	selector: "stash@{0}",
};

describe("DropStashDialog", () => {
	it("confirms the exact stash and permits cancellation", async () => {
		const onConfirm = vi.fn();
		const onTargetChange = vi.fn();
		render(
			<DropStashDialog
				busyAction={null}
				onConfirm={onConfirm}
				onTargetChange={onTargetChange}
				target={target}
			/>,
		);

		await userEvent.click(screen.getByRole("button", { name: "Delete stash" }));
		expect(onConfirm).toHaveBeenCalledWith(target);

		await userEvent.click(screen.getByRole("button", { name: "Cancel" }));
		expect(onTargetChange).toHaveBeenCalledWith(null);
	});

	it("cannot close or submit while deletion is running", () => {
		render(
			<DropStashDialog
				busyAction={StashAction.Drop}
				onConfirm={vi.fn()}
				onTargetChange={vi.fn()}
				target={target}
			/>,
		);

		expect(screen.getByRole("button", { name: "Cancel" })).toBeDisabled();
		expect(screen.getByRole("button", { name: "Delete stash" })).toBeDisabled();
	});
});
