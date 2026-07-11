// @vitest-environment jsdom

import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { CommitGraphRow } from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { gitMutationTimeoutMs } from "@/lib/gitMutationTimeout";
import { CreateTagDialog } from "./CreateTagDialog";

vi.mock("@/lib/commands", () => ({ sendRequestWithResponse: vi.fn() }));
vi.mock("sonner", () => ({
	toast: {
		error: vi.fn(),
		loading: vi.fn(() => "toast"),
		success: vi.fn(),
		warning: vi.fn(),
	},
}));

const send = vi.mocked(sendRequestWithResponse);

describe("CreateTagDialog signed tags", () => {
	beforeEach(() => send.mockReset());

	it("creates a signed annotated tag with the configured Git key", async () => {
		const user = userEvent.setup();
		const onRepositoryChanged = vi.fn();
		renderDialog({ onRepositoryChanged });
		await user.type(screen.getByLabelText("Tag name"), "v1.2.3");
		await user.click(
			screen.getByRole("checkbox", { name: "Annotated tag with a message" }),
		);
		await user.type(screen.getByLabelText("Tag message"), "Signed release");
		await user.click(
			screen.getByRole("checkbox", { name: "Cryptographically sign this tag" }),
		);
		await user.click(screen.getByRole("button", { name: "Create tag" }));

		expect(send).toHaveBeenCalledWith(
			{
				arguments: {
					commitHash: "a".repeat(40),
					isAnnotated: true,
					message: "Signed release",
					repositoryId: "repo",
					sign: true,
					tagName: "v1.2.3",
				},
				commandType: "CreateTagAtCommit",
			},
			{ timeoutMs: gitMutationTimeoutMs },
		);
		await waitFor(() => expect(onRepositoryChanged).toHaveBeenCalledOnce());
	});

	it("removes signing when annotation is disabled", async () => {
		const user = userEvent.setup();
		renderDialog({});
		const annotated = screen.getByRole("checkbox", {
			name: "Annotated tag with a message",
		});
		await user.click(annotated);
		await user.click(
			screen.getByRole("checkbox", { name: "Cryptographically sign this tag" }),
		);
		await user.click(annotated);
		await user.click(annotated);

		expect(
			screen.getByRole("checkbox", { name: "Cryptographically sign this tag" }),
		).not.toBeChecked();
	});

	it("keeps the dialog retryable after signing fails", async () => {
		const user = userEvent.setup();
		send.mockRejectedValueOnce(new Error("Git could not sign the tag"));
		renderDialog({});
		await user.type(screen.getByLabelText("Tag name"), "v-retry");
		await user.click(
			screen.getByRole("checkbox", { name: "Annotated tag with a message" }),
		);
		await user.type(screen.getByLabelText("Tag message"), "Release");
		await user.click(
			screen.getByRole("checkbox", { name: "Cryptographically sign this tag" }),
		);
		await user.click(screen.getByRole("button", { name: "Create tag" }));

		await waitFor(() =>
			expect(screen.getByRole("button", { name: "Create tag" })).toBeEnabled(),
		);
		expect(screen.getByLabelText("Tag name")).toHaveValue("v-retry");
		await user.click(screen.getByRole("button", { name: "Create tag" }));
		expect(send).toHaveBeenCalledTimes(2);
	});
});

function renderDialog({
	onRepositoryChanged = vi.fn(),
}: {
	onRepositoryChanged?: () => void;
}) {
	return render(
		<CreateTagDialog
			commit={{ commit: { hash: "a".repeat(40) } } as CommitGraphRow}
			existingTagNames={[]}
			onOpenChange={vi.fn()}
			onRepositoryChanged={onRepositoryChanged}
			remoteName={null}
			repositoryId="repo"
		/>,
	);
}
