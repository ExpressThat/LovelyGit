// @vitest-environment jsdom

import { act, renderHook, waitFor } from "@testing-library/react";
import { toast } from "sonner";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { sendRequestWithResponse } from "@/lib/commands";
import { nativeDialogTimeoutMs } from "@/lib/nativeDialogTimeout";
import { useInitializeRepository } from "./useInitializeRepository";

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

const send = vi.mocked(sendRequestWithResponse);

describe("useInitializeRepository", () => {
	beforeEach(() => vi.clearAllMocks());

	it("preserves a failed form and permits a successful retry", async () => {
		send.mockRejectedValueOnce(new Error("Destination already exists"));
		const { result } = renderHook(() => useInitializeRepository());
		populate(result.current);

		await act(() => result.current.initializeRepository());

		expect(result.current.isBusy).toBe(false);
		expect(result.current.open).toBe(true);
		expect(result.current.directoryName).toBe("project");
		expect(repositories.reloadRepositories).not.toHaveBeenCalled();
		expect(toast.error).toHaveBeenCalledWith("Destination already exists");

		send.mockResolvedValueOnce({
			id: "new-repo",
			name: "project",
			path: "C:\\temp\\project",
		});
		await act(() => result.current.initializeRepository());

		expect(repositories.reconcileRepository).toHaveBeenCalledWith({
			id: "new-repo",
			name: "project",
			path: "C:\\temp\\project",
		});
		expect(repositories.reloadRepositories).not.toHaveBeenCalled();
		expect(repositories.setCurrentRepositoryId).toHaveBeenCalledWith(
			"new-repo",
		);
		expect(result.current.open).toBe(false);
		expect(result.current.directoryName).toBe("");
		expect(result.current.initialBranchName).toBe("main");
	});

	it("does not send incomplete forms", async () => {
		const { result } = renderHook(() => useInitializeRepository());

		await act(() => result.current.initializeRepository());

		expect(send).not.toHaveBeenCalled();
		expect(repositories.reloadRepositories).not.toHaveBeenCalled();
	});

	it("keeps the created repository visible when opening it fails", async () => {
		const createdRepository = {
			id: "new-repo",
			name: "project",
			path: "C:\\temp\\project",
		};
		send.mockResolvedValueOnce(createdRepository);
		repositories.setCurrentRepositoryId.mockRejectedValueOnce(
			new Error("Could not save the current repository"),
		);
		const { result } = renderHook(() => useInitializeRepository());
		populate(result.current);

		await act(() => result.current.initializeRepository());

		expect(repositories.reconcileRepository).toHaveBeenCalledWith(
			createdRepository,
		);
		expect(repositories.reloadRepositories).not.toHaveBeenCalled();
		expect(result.current.open).toBe(true);
		expect(result.current.directoryName).toBe("project");
		expect(toast.error).toHaveBeenCalledWith(
			"Could not save the current repository",
		);
	});

	it("chooses a parent folder and surfaces picker failures", async () => {
		send.mockResolvedValueOnce({ parentPath: "C:\\projects" });
		const { result } = renderHook(() => useInitializeRepository());

		await act(() => result.current.chooseDestination());
		expect(result.current.parentPath).toBe("C:\\projects");
		expect(send).toHaveBeenLastCalledWith(
			{ commandType: "ChooseRepositoryDestination" },
			{ timeoutMs: nativeDialogTimeoutMs },
		);

		send.mockResolvedValueOnce(null);
		await act(() => result.current.chooseDestination());
		expect(result.current.parentPath).toBe("C:\\projects");

		send.mockRejectedValueOnce(new Error("Picker unavailable"));
		await act(() => result.current.chooseDestination());
		expect(toast.error).toHaveBeenCalledWith("Picker unavailable");
	});

	it("prevents duplicate submissions while creation is running", async () => {
		const request = deferred();
		send.mockReturnValueOnce(request.promise);
		const { result } = renderHook(() => useInitializeRepository());
		populate(result.current);

		let operation!: Promise<void>;
		act(() => {
			operation = result.current.initializeRepository();
		});
		await waitFor(() => expect(result.current.isBusy).toBe(true));
		await act(() => result.current.initializeRepository());
		expect(send).toHaveBeenCalledOnce();

		await act(async () => {
			request.reject(new Error("Failed"));
			await operation;
		});
		expect(result.current.isBusy).toBe(false);
	});
});

function populate(result: ReturnType<typeof useInitializeRepository>) {
	act(() => {
		result.setOpen(true);
		result.setDirectoryName("project");
		result.setParentPath("C:\\temp");
	});
}

function deferred() {
	let reject!: (error: Error) => void;
	const promise = new Promise<never>((_, decline) => {
		reject = decline;
	});
	return { promise, reject };
}
