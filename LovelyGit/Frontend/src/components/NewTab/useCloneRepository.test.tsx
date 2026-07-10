// @vitest-environment jsdom

import { act, renderHook, waitFor } from "@testing-library/react";
import { toast } from "sonner";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import {
	sendRequestWithResponse,
	subscribeToServerEvent,
} from "@/lib/commands";
import { useCloneRepository } from "./useCloneRepository";

const repositoryMocks = vi.hoisted(() => ({
	reloadRepositories: vi.fn(),
	setCurrentRepositoryId: vi.fn(),
}));

vi.mock("@/lib/repositoryContext", () => ({
	useRepositoryContext: () => repositoryMocks,
}));
vi.mock("@/lib/commands", () => ({
	sendRequestWithResponse: vi.fn(),
	subscribeToServerEvent: vi.fn(() => vi.fn()),
}));
vi.mock("sonner", () => ({
	toast: { error: vi.fn(), info: vi.fn(), success: vi.fn() },
}));

const send = vi.mocked(sendRequestWithResponse);
const subscribe = vi.mocked(subscribeToServerEvent);
const operationId = "11111111-1111-4111-8111-111111111111";

describe("useCloneRepository", () => {
	beforeEach(() => {
		vi.clearAllMocks();
		vi.spyOn(globalThis.crypto, "randomUUID").mockReturnValue(operationId);
	});
	afterEach(() => vi.restoreAllMocks());

	it("preserves a failed form and permits a successful retry", async () => {
		send.mockRejectedValueOnce(new Error("Authentication failed"));
		const { result } = renderHook(() => useCloneRepository());
		populateForm(result.current);

		await act(() => result.current.cloneRepository());

		expect(result.current.status).toBe("idle");
		expect(result.current.open).toBe(true);
		expect(result.current.remoteUrl).toBe("https://example.test/team/repo.git");
		expect(repositoryMocks.reloadRepositories).not.toHaveBeenCalled();
		expect(repositoryMocks.setCurrentRepositoryId).not.toHaveBeenCalled();
		expect(toast.error).toHaveBeenCalledWith("Authentication failed");

		send.mockResolvedValueOnce({
			id: "new-repo",
			name: "repo",
			path: "C:\\temp\\repo",
		});
		await act(() => result.current.cloneRepository());

		expect(repositoryMocks.reloadRepositories).toHaveBeenCalledOnce();
		expect(repositoryMocks.setCurrentRepositoryId).toHaveBeenCalledWith(
			"new-repo",
		);
		expect(result.current.open).toBe(false);
		expect(result.current.remoteUrl).toBe("");
	});

	it("tracks only its operation and reports active cancellation", async () => {
		const clone = deferred<never>();
		send.mockImplementationOnce(() => clone.promise);
		const { result } = renderHook(() => useCloneRepository());
		populateForm(result.current);
		let cloneRequest!: Promise<void>;
		act(() => {
			cloneRequest = result.current.cloneRepository();
		});
		await waitFor(() => expect(result.current.status).toBe("cloning"));

		const progressHandler = subscribe.mock.calls[0]?.[1];
		act(() => {
			progressHandler?.({
				message: "Wrong clone",
				operationId: "22222222-2222-4222-8222-222222222222",
				percent: 50,
				phasePercent: 50,
				stage: "Receiving objects",
			});
		});
		expect(result.current.progress?.message).toBe("Preparing destination");

		act(() => {
			progressHandler?.({
				message: "Receiving objects",
				operationId,
				percent: 40,
				phasePercent: 75,
				stage: "Receiving objects",
			});
		});
		expect(result.current.progress?.percent).toBe(40);

		send.mockResolvedValueOnce(undefined);
		await act(() => result.current.cancelClone());
		expect(result.current.status).toBe("canceling");
		expect(result.current.progress?.stage).toBe("Canceling");
		expect(send).toHaveBeenLastCalledWith({
			arguments: { operationId },
			commandType: "CancelCloneRepository",
		});

		await act(async () => {
			clone.reject(new Error("Clone canceled"));
			await cloneRequest;
		});
		expect(toast.info).toHaveBeenCalledWith("Clone canceled");
		expect(result.current.status).toBe("idle");
		expect(result.current.open).toBe(true);
	});

	it("restores cloning state when cancellation itself fails", async () => {
		const clone = deferred<never>();
		send.mockImplementationOnce(() => clone.promise);
		const { result } = renderHook(() => useCloneRepository());
		populateForm(result.current);
		let cloneRequest!: Promise<void>;
		act(() => {
			cloneRequest = result.current.cloneRepository();
		});
		await waitFor(() => expect(result.current.status).toBe("cloning"));

		send.mockRejectedValueOnce(new Error("Cancellation unavailable"));
		await act(() => result.current.cancelClone());

		expect(result.current.status).toBe("cloning");
		expect(toast.error).toHaveBeenCalledWith("Cancellation unavailable");
		await act(async () => {
			clone.reject(new Error("Network failed"));
			await cloneRequest;
		});
		expect(toast.error).toHaveBeenCalledWith("Network failed");
	});
});

function populateForm(result: ReturnType<typeof useCloneRepository>) {
	act(() => {
		result.setOpen(true);
		result.updateRemoteUrl("https://example.test/team/repo.git");
		result.setParentPath("C:\\temp");
	});
}

function deferred<T>() {
	let reject!: (reason: Error) => void;
	let resolve!: (value: T) => void;
	const promise = new Promise<T>((accept, decline) => {
		resolve = accept;
		reject = decline;
	});
	return { promise, reject, resolve };
}
