// @vitest-environment jsdom

import { act, renderHook, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type {
	CommitChangedFile,
	CommitDetailsResponse,
	RepositoryStashItem,
} from "@/generated/types";
import { loadCommitDetails } from "@/lib/commitDetailsCache";
import { useStashInspection } from "./useStashInspection";

vi.mock("@/lib/commitDetailsCache", () => ({ loadCommitDetails: vi.fn() }));
const load = vi.mocked(loadCommitDetails);

const stash: RepositoryStashItem = {
	commitHash: "stash-commit",
	createdAtUnixSeconds: 1_700_000_000,
	message: "checkpoint",
	selector: "stash@{0}",
};

describe("useStashInspection", () => {
	beforeEach(() => vi.clearAllMocks());

	it("loads tracked files without looking for a missing untracked parent", async () => {
		load.mockResolvedValueOnce(
			details("stash-commit", ["head", "index"], [file("app.ts")]),
		);
		const { result } = renderHook(() => useStashInspection("repo", stash));

		await waitFor(() => expect(result.current.state.status).toBe("loaded"));
		expect(load).toHaveBeenCalledOnce();
		if (result.current.state.status !== "loaded") return;
		expect(result.current.state.untracked).toBeNull();
		expect(result.current.state.files).toEqual([
			expect.objectContaining({
				commitHash: "stash-commit",
				source: "Tracked",
			}),
		]);
	});

	it("combines the third-parent untracked snapshot with source identity", async () => {
		load
			.mockResolvedValueOnce(
				details(
					"stash-commit",
					["head", "index", "untracked"],
					[file("app.ts")],
				),
			)
			.mockResolvedValueOnce(
				details("untracked", [], [file("notes.txt", "Added")]),
			);
		const { result } = renderHook(() => useStashInspection("repo", stash));

		await waitFor(() => expect(result.current.state.status).toBe("loaded"));
		if (result.current.state.status !== "loaded") return;
		expect(
			result.current.state.files.map(({ commitHash, file: item, source }) => ({
				commitHash,
				path: item.path,
				source,
			})),
		).toEqual([
			{ commitHash: "stash-commit", path: "app.ts", source: "Tracked" },
			{ commitHash: "untracked", path: "notes.txt", source: "Untracked" },
		]);
	});

	it("surfaces a third-parent failure and retries the complete read", async () => {
		const tracked = details(
			"stash-commit",
			["head", "index", "untracked"],
			[file("app.ts")],
		);
		load
			.mockResolvedValueOnce(tracked)
			.mockRejectedValueOnce(new Error("Object missing"));
		const { result } = renderHook(() => useStashInspection("repo", stash));

		await waitFor(() =>
			expect(result.current.state).toEqual({
				message: "Object missing",
				status: "error",
			}),
		);
		load
			.mockResolvedValueOnce(tracked)
			.mockResolvedValueOnce(details("untracked", [], []));
		act(() => result.current.retry());
		await waitFor(() => expect(result.current.state.status).toBe("loaded"));
		expect(load).toHaveBeenCalledTimes(4);
	});

	it("ignores a late response after the selected stash changes", async () => {
		const first = deferred<CommitDetailsResponse>();
		const second = deferred<CommitDetailsResponse>();
		load.mockReturnValueOnce(first.promise).mockReturnValueOnce(second.promise);
		const { rerender, result } = renderHook(
			({ selected }) => useStashInspection("repo", selected),
			{ initialProps: { selected: stash } },
		);
		const next = { ...stash, commitHash: "new-stash", selector: "stash@{1}" };
		rerender({ selected: next });
		await act(() => {
			second.resolve(details("new-stash", ["head", "index"], [file("new.ts")]));
			return second.promise;
		});
		await act(() => {
			first.resolve(
				details("stash-commit", ["head", "index"], [file("old.ts")]),
			);
			return first.promise;
		});
		await waitFor(() => expect(result.current.state.status).toBe("loaded"));
		if (result.current.state.status !== "loaded") return;
		expect(result.current.state.files[0]?.file.path).toBe("new.ts");
	});
});

function details(
	hash: string,
	parents: string[],
	changedFiles: CommitChangedFile[],
): CommitDetailsResponse {
	return {
		author: "Tester",
		body: "",
		branches: [],
		changedFiles,
		date: 1,
		email: "test@example.com",
		hash,
		hasLineStats: true,
		message: "stash",
		parents,
		signatureKind: "None",
		stats: { additions: 1, deletions: 0 },
		subject: "stash",
		tags: [],
	};
}

function file(path: string, status = "Modified"): CommitChangedFile {
	return { additions: 1, deletions: 0, isBinary: false, path, status };
}

function deferred<T>() {
	let resolve!: (value: T) => void;
	const promise = new Promise<T>((complete) => {
		resolve = complete;
	});
	return { promise, resolve };
}
