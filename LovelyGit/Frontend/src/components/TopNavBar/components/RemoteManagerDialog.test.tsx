// @vitest-environment jsdom

import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { sendRequestWithResponse } from "@/lib/commands";
import { RemoteManagerDialog } from "./RemoteManagerDialog";

vi.mock("@/lib/commands", () => ({ sendRequestWithResponse: vi.fn() }));

const origin = {
	name: "origin",
	pushUrl: "git@example.invalid:team/repo.git",
	url: "https://example.invalid/team/repo.git",
};

describe("RemoteManagerDialog", () => {
	beforeEach(() => {
		vi.clearAllMocks();
		vi.mocked(sendRequestWithResponse).mockResolvedValue([origin]);
	});

	it("renders native remote details and opens the add editor", async () => {
		const user = userEvent.setup();
		renderDialog();

		expect(await screen.findByText("origin")).toBeVisible();
		expect(screen.getByText(`Fetch · ${origin.url}`)).toBeVisible();
		expect(screen.getByText(`Push · ${origin.pushUrl}`)).toBeVisible();
		await user.click(screen.getByRole("button", { name: "Add remote" }));
		expect(
			screen.getByRole("heading", { name: "Add remote" }),
		).toBeInTheDocument();
	});

	it("bounds rendering for repositories with many remotes", async () => {
		vi.mocked(sendRequestWithResponse).mockResolvedValueOnce(
			Array.from({ length: 500 }, (_, index) => ({
				name: `remote-${index}`,
				pushUrl: `ssh://example.invalid/${index}`,
				url: `https://example.invalid/${index}`,
			})),
		);
		renderDialog();

		expect(await screen.findByText("remote-0")).toBeVisible();
		expect(screen.getAllByRole("listitem")).toHaveLength(10);
		expect(screen.queryByText("remote-499")).toBeNull();
	});

	it("opens a destructive confirmation without closing the manager", async () => {
		const user = userEvent.setup();
		renderDialog();
		await screen.findByText("origin");

		await user.click(screen.getByRole("button", { name: "Remove origin" }));

		expect(screen.getByText("Remove origin?")).toBeVisible();
		expect(screen.getByText("Manage remotes")).toBeVisible();
		expect(screen.getByRole("button", { name: "Remove remote" })).toBeVisible();
	});

	it("does not close while a mutation is active", async () => {
		let resolveMutation: (() => void) | undefined;
		vi.mocked(sendRequestWithResponse)
			.mockResolvedValueOnce([origin])
			.mockImplementationOnce(
				() => new Promise<void>((resolve) => (resolveMutation = resolve)),
			);
		const user = userEvent.setup();
		renderDialog();
		await screen.findByText("origin");
		await user.click(screen.getByRole("button", { name: "Remove origin" }));
		await user.click(screen.getByRole("button", { name: "Remove remote" }));

		await waitFor(() =>
			expect(screen.getByRole("button", { name: "Removing" })).toBeDisabled(),
		);
		resolveMutation?.();
	});
});

function renderDialog() {
	render(
		<RemoteManagerDialog onOpenChange={vi.fn()} open repositoryId="repo" />,
	);
}
