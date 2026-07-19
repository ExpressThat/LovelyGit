// @vitest-environment jsdom

import { act, render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { KnownGitRepository } from "@/generated/types";
import {
	RepositoryProvider,
	resolveCurrentRepositoryId,
	upsertRepository,
	useRepositoryContext,
} from "./repositoryContext";

const state = vi.hoisted(() => ({
	closeError: vi.fn(),
	currentRepositoryId: "missing" as string | null,
	initSettingsStore: vi.fn(async () => undefined),
	sendRequestWithResponse: vi.fn(),
	setSetting: vi.fn(async () => undefined),
}));

vi.mock("@/lib/commands", () => ({
	sendRequestWithResponse: state.sendRequestWithResponse,
}));

vi.mock("@/lib/settings/settingsStore", () => ({
	initSettingsStore: state.initSettingsStore,
	setSetting: state.setSetting,
	useSetting: () => state.currentRepositoryId,
}));

describe("RepositoryProvider", () => {
	beforeEach(() => {
		state.currentRepositoryId = "missing";
		vi.clearAllMocks();
	});

	it("clears a persisted repository id that is absent after loading", async () => {
		let finishLoading: () => void = () => {};
		state.sendRequestWithResponse.mockReturnValueOnce(
			new Promise((resolve) => {
				finishLoading = () =>
					resolve({
						compactRepositoriesGzipBase64: null,
						repositories: [],
					});
			}),
		);
		renderProvider();

		expect(screen.getByTestId("current-repository")).toHaveTextContent(
			"missing",
		);
		await act(async () => finishLoading());

		await waitFor(() =>
			expect(screen.getByTestId("current-repository")).toHaveTextContent(
				"none",
			),
		);
		expect(state.setSetting).toHaveBeenCalledWith(
			"CurrentGitRepositoryId",
			null,
		);
	});

	it("retains a persisted repository id that resolves successfully", async () => {
		state.currentRepositoryId = "repository-1";
		state.sendRequestWithResponse.mockResolvedValueOnce({
			compactRepositoriesGzipBase64: null,
			repositories: [repository("repository-1")],
		});
		renderProvider();

		await waitFor(() =>
			expect(screen.getByTestId("current-repository")).toHaveTextContent(
				"repository-1",
			),
		);
		expect(state.setSetting).not.toHaveBeenCalled();
	});

	it("retains the stored id without leaking an initial list failure", async () => {
		state.currentRepositoryId = "repository-1";
		state.sendRequestWithResponse.mockRejectedValueOnce(
			new Error("Repository list unavailable"),
		);
		renderProvider();

		await waitFor(() =>
			expect(state.sendRequestWithResponse).toHaveBeenCalledOnce(),
		);
		expect(screen.getByTestId("current-repository")).toHaveTextContent(
			"repository-1",
		);
		expect(state.setSetting).not.toHaveBeenCalled();
	});

	it("retains the stored id until a repository load succeeds", () => {
		expect(resolveCurrentRepositoryId("repository-1", null, false)).toBe(
			"repository-1",
		);
	});

	it("adds and replaces returned repositories without reloading the list", () => {
		const first = repository("repository-1");
		const replacement = { ...first, name: "Renamed" };
		const repositories = [first];

		expect(upsertRepository([], first)).toEqual([first]);
		expect(upsertRepository(repositories, replacement)).toEqual([replacement]);
		expect(upsertRepository(repositories, first)).toBe(repositories);
	});

	it("removes locally after native success without reloading the full list", async () => {
		state.currentRepositoryId = "repository-1";
		const pending = deferred<void>();
		state.sendRequestWithResponse
			.mockResolvedValueOnce(
				directResponse([
					repository("repository-1"),
					repository("repository-2"),
				]),
			)
			.mockReturnValueOnce(pending.promise);
		renderProvider();
		await screen.findByText("repository-1,repository-2");

		act(() => screen.getByRole("button", { name: "Remove second" }).click());
		act(() => screen.getByRole("button", { name: "Add third" }).click());
		await act(async () => pending.resolve());

		await screen.findByText("repository-1,repository-3");
		expect(state.sendRequestWithResponse).toHaveBeenCalledTimes(2);
		expect(state.sendRequestWithResponse).toHaveBeenLastCalledWith({
			arguments: { knownRepositoryId: "repository-2" },
			commandType: "RemoveKnownGitRepositorys",
		});
	});

	it("preserves the local repository when native removal fails", async () => {
		state.currentRepositoryId = "repository-1";
		state.sendRequestWithResponse
			.mockResolvedValueOnce(
				directResponse([
					repository("repository-1"),
					repository("repository-2"),
				]),
			)
			.mockRejectedValueOnce(new Error("Database unavailable"));
		renderProvider();
		await screen.findByText("repository-1,repository-2");

		act(() => screen.getByRole("button", { name: "Remove second" }).click());

		await waitFor(() => expect(state.closeError).toHaveBeenCalled());
		expect(screen.getByTestId("repositories")).toHaveTextContent(
			"repository-1,repository-2",
		);
	});
});

function renderProvider() {
	return render(
		<RepositoryProvider>
			<CurrentRepositoryProbe />
		</RepositoryProvider>,
	);
}

function CurrentRepositoryProbe() {
	const {
		closeRepository,
		currentRepositoryId,
		reconcileRepository,
		repositories,
	} = useRepositoryContext();
	return (
		<>
			<span data-testid="current-repository">
				{currentRepositoryId ?? "none"}
			</span>
			<span data-testid="repositories">
				{repositories.map((item) => item.id).join(",")}
			</span>
			<button
				onClick={() =>
					void closeRepository("repository-2").catch(state.closeError)
				}
				type="button"
			>
				Remove second
			</button>
			<button
				onClick={() => reconcileRepository(repository("repository-3"))}
				type="button"
			>
				Add third
			</button>
		</>
	);
}

function directResponse(repositories: KnownGitRepository[]) {
	return { compactRepositoriesGzipBase64: null, repositories };
}

function deferred<T>() {
	let resolve!: (value: T) => void;
	const promise = new Promise<T>((resolvePromise) => {
		resolve = resolvePromise;
	});
	return { promise, resolve };
}

function repository(id: string): KnownGitRepository {
	return {
		id,
		name: "Repository",
		path: "C:\\Repositories\\repository",
	};
}
