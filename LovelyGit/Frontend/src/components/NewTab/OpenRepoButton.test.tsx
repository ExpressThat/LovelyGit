// @vitest-environment jsdom

import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { toast } from "sonner";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { sendRequestWithResponse } from "@/lib/commands";
import { nativeDialogTimeoutMs } from "@/lib/nativeDialogTimeout";
import { OpenRepoButton } from "./OpenRepoButton";

const repositories = vi.hoisted(() => ({
	reconcileRepository: vi.fn(),
	reloadRepositories: vi.fn(),
	setCurrentRepositoryId: vi.fn(),
}));

vi.mock("@/lib/repositoryContext", () => ({
	useRepositoryContext: () => repositories,
}));
vi.mock("@/lib/commands", () => ({ sendRequestWithResponse: vi.fn() }));
vi.mock("sonner", () => ({ toast: { error: vi.fn() } }));

const send = vi.mocked(sendRequestWithResponse);

describe("OpenRepoButton", () => {
	beforeEach(() => vi.clearAllMocks());

	it("keeps the button busy while selecting and opens the repository", async () => {
		const user = userEvent.setup();
		const selection = deferred<{ id: string; name: string; path: string }>();
		send.mockReturnValueOnce(selection.promise);
		render(<OpenRepoButton />);

		await user.click(screen.getByRole("button", { name: "Open Repo" }));
		expect(screen.getByRole("button", { name: "Selecting" })).toBeDisabled();
		expect(send).toHaveBeenCalledWith(
			{ commandType: "AddKnownGitRepositorys" },
			{ timeoutMs: nativeDialogTimeoutMs },
		);

		selection.resolve({ id: "repo-1", name: "demo", path: "C:/demo" });
		await waitFor(() =>
			expect(repositories.setCurrentRepositoryId).toHaveBeenCalledWith(
				"repo-1",
			),
		);
		expect(repositories.reconcileRepository).toHaveBeenCalledWith({
			id: "repo-1",
			name: "demo",
			path: "C:/demo",
		});
		expect(repositories.reloadRepositories).not.toHaveBeenCalled();
		expect(screen.getByRole("button", { name: "Open Repo" })).toBeEnabled();
	});

	it("treats picker cancellation as a clean no-op", async () => {
		const user = userEvent.setup();
		send.mockResolvedValueOnce(null);
		render(<OpenRepoButton />);

		await user.click(screen.getByRole("button", { name: "Open Repo" }));
		await waitFor(() =>
			expect(screen.getByRole("button", { name: "Open Repo" })).toBeEnabled(),
		);
		expect(repositories.reloadRepositories).not.toHaveBeenCalled();
		expect(toast.error).not.toHaveBeenCalled();
	});

	it("retains the added repository when automatic opening fails", async () => {
		const user = userEvent.setup();
		const repository = { id: "repo-1", name: "demo", path: "C:/demo" };
		send.mockResolvedValueOnce(repository);
		repositories.setCurrentRepositoryId.mockRejectedValueOnce(
			new Error("Could not select repository"),
		);
		render(<OpenRepoButton />);

		await user.click(screen.getByRole("button", { name: "Open Repo" }));

		await waitFor(() =>
			expect(toast.error).toHaveBeenCalledWith("Could not select repository"),
		);
		expect(repositories.reconcileRepository).toHaveBeenCalledWith(repository);
		expect(repositories.reloadRepositories).not.toHaveBeenCalled();
		expect(screen.getByRole("button", { name: "Open Repo" })).toBeEnabled();
	});

	it("surfaces a picker failure and permits retry", async () => {
		const user = userEvent.setup();
		send.mockRejectedValueOnce(new Error("Picker unavailable"));
		render(<OpenRepoButton />);

		await user.click(screen.getByRole("button", { name: "Open Repo" }));
		await waitFor(() =>
			expect(toast.error).toHaveBeenCalledWith("Picker unavailable"),
		);
		expect(screen.getByRole("button", { name: "Open Repo" })).toBeEnabled();

		send.mockResolvedValueOnce(null);
		await user.click(screen.getByRole("button", { name: "Open Repo" }));
		expect(send).toHaveBeenCalledTimes(2);
	});
});

function deferred<T>() {
	let resolve!: (value: T) => void;
	const promise = new Promise<T>((complete) => {
		resolve = complete;
	});
	return { promise, resolve };
}
