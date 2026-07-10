// @vitest-environment jsdom

import { fireEvent, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import type { GitReflogEntry } from "@/generated/types";
import { ReflogEntryRow } from "./ReflogEntryRow";

describe("ReflogEntryRow", () => {
	it("offers visible branch recovery and reset actions", async () => {
		const user = userEvent.setup();
		const entry = reflogEntry();
		const onCreateBranch = vi.fn();
		const onReset = vi.fn();
		render(
			<ReflogEntryRow
				entry={entry}
				onCopy={vi.fn()}
				onCreateBranch={onCreateBranch}
				onReset={onReset}
			/>,
		);

		await user.click(
			screen.getByRole("button", {
				name: "Create recovery branch at main@{0}",
			}),
		);
		await user.click(
			screen.getByRole("button", { name: "Reset current branch to main@{0}" }),
		);
		expect(onCreateBranch).toHaveBeenCalledWith(entry);
		expect(onReset).toHaveBeenCalledWith(entry);
	});

	it("exposes the same recovery actions from its context menu", async () => {
		const user = userEvent.setup();
		const entry = reflogEntry();
		const onCopy = vi.fn();
		render(
			<ReflogEntryRow
				entry={entry}
				onCopy={onCopy}
				onCreateBranch={vi.fn()}
				onReset={vi.fn()}
			/>,
		);

		fireEvent.contextMenu(screen.getByText(entry.message));
		await user.click(await screen.findByText("Copy commit hash"));
		expect(onCopy).toHaveBeenCalledWith(entry.newHash);
	});
});

function reflogEntry(): GitReflogEntry {
	return {
		actorEmail: "ross@example.invalid",
		actorName: "Ross",
		message: "reset: moving to HEAD~1",
		newHash: "abcdef1234567890abcdef1234567890abcdef12",
		oldHash: "1234567890abcdef1234567890abcdef12345678",
		selector: "main@{0}",
		timestampUnixSeconds: 1_700_000_000,
		timezone: "+0000",
	};
}
