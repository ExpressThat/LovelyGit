// @vitest-environment jsdom

import { act, fireEvent, render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type {
	WorkingTreeChangedFile,
	WorkingTreeChangesResponse,
} from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { WorkingChangesPanel } from "./WorkingChangesPanel";

vi.mock("@/lib/commands", async (importOriginal) => ({
	...(await importOriginal<typeof import("@/lib/commands")>()),
	sendRequestWithResponse: vi.fn(),
}));

describe("WorkingChangesPanel optimistic index state", () => {
	beforeEach(() => vi.clearAllMocks());

	it("keeps a staged preview when an intermediate status event replaces the parent response", async () => {
		const command = deferred<unknown>();
		const refresh = deferred<void>();
		vi.mocked(sendRequestWithResponse).mockReturnValue(command.promise);
		const props = createProps(() => refresh.promise);
		const view = render(
			<WorkingChangesPanel {...props} changes={response()} />,
		);

		fireEvent.click(screen.getByRole("button", { name: "Stage all changes" }));
		expect(
			screen.getByRole("heading", { name: "Staged files (1)" }),
		).toBeTruthy();

		view.rerender(<WorkingChangesPanel {...props} changes={response()} />);
		expect(
			screen.getByRole("heading", { name: "Staged files (1)" }),
		).toBeTruthy();
		expect(
			screen.getByRole("heading", { name: "Unstaged files (0)" }),
		).toBeTruthy();

		await act(async () => command.resolve(undefined));
		expect(props.onRefresh).toHaveBeenCalledOnce();
		await act(async () => refresh.resolve());
	});

	it("disables mutations while untracked discovery completes", () => {
		const changes = response();
		changes.isComplete = false;
		render(
			<WorkingChangesPanel
				{...createProps(() => Promise.resolve())}
				changes={changes}
			/>,
		);

		expect(
			screen.getByRole("button", { name: "Stage all changes" }),
		).toBeDisabled();
		expect(screen.getByText("Finding untracked files…")).toBeVisible();
	});
});

function createProps(onRefresh: () => Promise<void>) {
	return {
		error: null,
		isLoading: false,
		onCommitSuccess: vi.fn(),
		onOpenFileBlame: vi.fn(),
		onOpenFileHistory: vi.fn(),
		onRefresh: vi.fn(onRefresh),
		onSelectFile: vi.fn(),
		repositoryId: "repo",
		totalCount: 1,
	};
}

function response(): WorkingTreeChangesResponse {
	return {
		isComplete: true,
		staged: [],
		totalCount: 1,
		unmerged: [],
		unstaged: [file()],
		untracked: [],
	};
}

function file(): WorkingTreeChangedFile {
	return {
		additions: 1,
		deletions: 0,
		group: "Unstaged",
		isBinary: false,
		oldPath: null,
		path: "file.ts",
		status: "Modified",
	};
}

function deferred<T>() {
	let resolve = (_value: T) => undefined;
	const promise = new Promise<T>((complete) => {
		resolve = (value) => {
			complete(value);
			return undefined;
		};
	});
	return { promise, resolve };
}
