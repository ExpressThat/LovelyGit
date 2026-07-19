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
		let finishLoading: (repositories: KnownGitRepository[]) => void = () => {};
		state.sendRequestWithResponse.mockReturnValueOnce(
			new Promise<KnownGitRepository[]>((resolve) => {
				finishLoading = resolve;
			}),
		);
		renderProvider();

		expect(screen.getByTestId("current-repository")).toHaveTextContent(
			"missing",
		);
		await act(async () => finishLoading([]));

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
		state.sendRequestWithResponse.mockResolvedValueOnce([
			repository("repository-1"),
		]);
		renderProvider();

		await waitFor(() =>
			expect(screen.getByTestId("current-repository")).toHaveTextContent(
				"repository-1",
			),
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
});

function renderProvider() {
	return render(
		<RepositoryProvider>
			<CurrentRepositoryProbe />
		</RepositoryProvider>,
	);
}

function CurrentRepositoryProbe() {
	const { currentRepositoryId } = useRepositoryContext();
	return (
		<span data-testid="current-repository">
			{currentRepositoryId ?? "none"}
		</span>
	);
}

function repository(id: string): KnownGitRepository {
	return {
		id,
		name: "Repository",
		path: "C:\\Repositories\\repository",
	};
}
