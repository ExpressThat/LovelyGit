// @vitest-environment jsdom

import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { CommitStagedForm } from "./CommitStagedForm";

vi.mock("./CommitIdentityControl", () => ({
	CommitIdentityControl: () => <div>Commit identity</div>,
}));

describe("CommitStagedForm", () => {
	it("offers amend mode with an explicit history rewrite warning", async () => {
		const user = userEvent.setup();
		const onAmendChange = vi.fn();
		renderForm({ onAmendChange });

		await user.click(
			screen.getByRole("switch", { name: /^Amend last commit/ }),
		);

		expect(onAmendChange.mock.calls[0]?.[0]).toBe(true);
		expect(
			screen.getByText(
				"Rewrite HEAD with this message and any staged changes.",
			),
		).toBeVisible();
	});

	it("describes the rewritten hash and changes the primary action", () => {
		renderForm({ isAmending: true });

		expect(
			screen.getByText(
				"Amending replaces the current commit and changes its hash.",
			),
		).toBeVisible();
		expect(screen.getByRole("button", { name: "Amend commit" })).toBeEnabled();
	});

	it("replaces the switch with a named loader while fetching HEAD", () => {
		renderForm({ isLoadingAmendMessage: true });

		expect(screen.getByLabelText("Loading last commit")).toBeVisible();
		expect(
			screen.queryByRole("switch", { name: /^Amend last commit/ }),
		).not.toBeInTheDocument();
	});

	it("rejects blank titles and unavailable commit state", () => {
		renderForm({ commitTitle: "   " });
		expect(screen.getByRole("button", { name: "Commit" })).toBeDisabled();

		renderForm({ canCommit: false, commitTitle: "Ready" });
		expect(screen.getAllByRole("button", { name: "Commit" })[1]).toBeDisabled();
	});

	it("disables every editable control while a mutation is active", () => {
		renderForm({ isBusy: true });

		expect(screen.getByLabelText("Commit title")).toBeDisabled();
		expect(screen.getByLabelText("Commit body")).toBeDisabled();
		expect(
			screen.getByRole("switch", { name: /^Amend last commit/ }),
		).toHaveAttribute("aria-disabled", "true");
		expect(screen.getByRole("button", { name: "Commit" })).toBeDisabled();
		expect(
			screen.getByRole("button", { name: "Undo last commit" }),
		).toBeDisabled();
	});

	it("opens the undo workflow from a dedicated secondary action", async () => {
		const user = userEvent.setup();
		const onUndo = vi.fn();
		renderForm({ onUndo });

		await user.click(screen.getByRole("button", { name: "Undo last commit" }));

		expect(onUndo).toHaveBeenCalledOnce();
	});

	it("allows signing to be overridden for this commit", async () => {
		const user = userEvent.setup();
		const onSigningChange = vi.fn();
		renderForm({ isSigningCommit: true, onSigningChange });
		const signing = screen.getByRole("switch", { name: /^Sign this commit/ });
		expect(signing).toBeChecked();
		await user.click(signing);
		expect(onSigningChange.mock.calls[0]?.[0]).toBe(false);
	});
});

function renderForm(
	overrides: Partial<Parameters<typeof CommitStagedForm>[0]> = {},
) {
	render(
		<CommitStagedForm
			canCommit
			commitBody="Body"
			commitTitle="Title"
			isAmending={false}
			isBusy={false}
			isCommitting={false}
			isLoadingAmendMessage={false}
			isSigningCommit={false}
			onAmendChange={vi.fn()}
			onCommit={vi.fn()}
			onCommitBodyChange={vi.fn()}
			onCommitTitleChange={vi.fn()}
			onSigningChange={vi.fn()}
			onUndo={vi.fn()}
			repositoryId="repository-1"
			{...overrides}
		/>,
	);
}
