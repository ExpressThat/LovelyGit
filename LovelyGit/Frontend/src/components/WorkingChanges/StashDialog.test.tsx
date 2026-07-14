// @vitest-environment jsdom

import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { sendRequestWithResponse } from "@/lib/commands";
import { clearRepositoryRefsCache } from "@/lib/repositoryRefsCache";
import { StashDialog } from "./StashDialog";

vi.mock("@/lib/commands", () => ({ sendRequestWithResponse: vi.fn() }));

const send = vi.mocked(sendRequestWithResponse);

describe("StashDialog selective scope", () => {
	beforeEach(() => {
		clearRepositoryRefsCache();
		vi.clearAllMocks();
		send.mockResolvedValue({ refs: [], stashes: [] });
	});

	it("defaults to the paths selected in working changes", async () => {
		render(<Subject selectedPaths={["main.txt", "new file.txt"]} />);

		await userEvent.click(screen.getByRole("button", { name: "Stash" }));

		expect(
			await screen.findByRole("button", { name: "Selected files (2)" }),
		).toHaveAttribute("aria-pressed", "true");
		expect(screen.getByRole("button", { name: "Stash changes" })).toBeEnabled();
	});

	it("cannot fall back to all changes when the active selection disappears", async () => {
		const { rerender } = render(<Subject selectedPaths={["main.txt"]} />);
		await userEvent.click(screen.getByRole("button", { name: "Stash" }));
		await screen.findByRole("button", { name: "Selected files (1)" });

		rerender(<Subject selectedPaths={[]} />);

		await waitFor(() =>
			expect(
				screen.getByRole("button", { name: "Stash changes" }),
			).toBeDisabled(),
		);
		expect(
			screen.getByRole("button", { name: "Selected files (0)" }),
		).toHaveAttribute("aria-pressed", "true");
	});
});

function Subject({ selectedPaths }: { selectedPaths: string[] }) {
	return (
		<StashDialog
			canCreate
			onRepositoryChanged={vi.fn()}
			repositoryId="repo"
			selectedPaths={selectedPaths}
		/>
	);
}
