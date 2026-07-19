// @vitest-environment jsdom

import { fireEvent, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { sendRequestWithResponse } from "@/lib/commands";
import { InitializeRepositoryDialog } from "./InitializeRepositoryDialog";

const repositories = vi.hoisted(() => ({
	reconcileRepository: vi.fn(),
	reloadRepositories: vi.fn(),
	setCurrentRepositoryId: vi.fn(),
}));

vi.mock("@/lib/repositoryContext", () => ({
	useRepositoryContext: () => repositories,
}));
vi.mock("@/lib/commands", () => ({ sendRequestWithResponse: vi.fn() }));
vi.mock("sonner", () => ({
	toast: { error: vi.fn(), success: vi.fn() },
}));

describe("InitializeRepositoryDialog", () => {
	beforeEach(() => vi.clearAllMocks());

	it("collects repository details and initializes through the typed command", async () => {
		const user = userEvent.setup();
		vi.mocked(sendRequestWithResponse).mockResolvedValueOnce({
			id: "repo-id",
			name: "lovely-project",
			path: "C:\\projects\\lovely-project",
		});
		render(<InitializeRepositoryDialog />);

		await user.click(screen.getByRole("button", { name: "New Repository" }));
		await user.type(screen.getByLabelText("Repository name"), "lovely-project");
		await user.type(screen.getByLabelText("Location"), "C:\\projects");
		const create = screen.getByRole("button", { name: "Create and open" });
		expect(create).toBeEnabled();
		await user.click(create);

		expect(sendRequestWithResponse).toHaveBeenCalledWith({
			arguments: {
				directoryName: "lovely-project",
				initialBranchName: "main",
				parentPath: "C:\\projects",
			},
			commandType: "InitializeRepository",
		});
		expect(repositories.setCurrentRepositoryId).toHaveBeenCalledWith("repo-id");
	});

	it("keeps creation disabled until all required fields are present", async () => {
		const user = userEvent.setup();
		render(<InitializeRepositoryDialog />);

		await user.click(screen.getByRole("button", { name: "New Repository" }));
		expect(
			screen.getByRole("button", { name: "Create and open" }),
		).toBeDisabled();
		await user.type(screen.getByLabelText("Repository name"), "project");
		expect(
			screen.getByRole("button", { name: "Create and open" }),
		).toBeDisabled();

		fireEvent.input(screen.getByLabelText("Location"), {
			target: { value: "C:\\projects" },
		});
		expect(
			screen.getByRole("button", { name: "Create and open" }),
		).toBeEnabled();
	});
});
