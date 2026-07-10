// @vitest-environment jsdom

import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { RemoteEditor } from "./RemoteEditor";
import type { RemoteDraft } from "./useRemoteManager";

describe("RemoteEditor", () => {
	it("prevents duplicate remote names", async () => {
		const user = userEvent.setup();
		renderEditor({ existingNames: ["origin"] });

		await user.type(screen.getByLabelText("Name"), "origin");
		await user.type(
			screen.getByLabelText("Fetch URL"),
			"https://example.invalid/repo.git",
		);

		expect(
			screen.getByText("A remote with this name already exists."),
		).toBeVisible();
		expect(screen.getByRole("button", { name: "Save remote" })).toBeDisabled();
	});

	it("submits trimmed-capable HTTPS and SSH fields", async () => {
		const user = userEvent.setup();
		const onSave = vi.fn();
		renderEditor({ onSave });

		await user.type(screen.getByLabelText("Name"), "upstream");
		await user.type(
			screen.getByLabelText("Fetch URL"),
			"git@example.invalid:team/repo.git",
		);
		await user.type(
			screen.getByLabelText("Push URL (optional)"),
			"ssh://example.invalid/repo.git",
		);
		await user.click(screen.getByRole("button", { name: "Save remote" }));

		expect(onSave).toHaveBeenCalledWith({
			name: "upstream",
			originalName: null,
			pushUrl: "ssh://example.invalid/repo.git",
			url: "git@example.invalid:team/repo.git",
		});
	});
});

function renderEditor({
	existingNames = [],
	onSave = vi.fn<(draft: RemoteDraft) => void>(),
}: {
	existingNames?: string[];
	onSave?: (draft: RemoteDraft) => void;
} = {}) {
	render(
		<RemoteEditor
			draft={{ name: "", originalName: null, pushUrl: "", url: "" }}
			existingNames={existingNames}
			isSaving={false}
			onCancel={vi.fn()}
			onSave={onSave}
		/>,
	);
}
