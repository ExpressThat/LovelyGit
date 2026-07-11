// @vitest-environment jsdom

import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { sendRequestWithResponse } from "@/lib/commands";
import { RemoteActionsControl } from "./RemoteActionsControl";

const toast = vi.hoisted(() => ({
	error: vi.fn(),
	loading: vi.fn(() => "toast-1"),
	success: vi.fn(),
}));
const sync = vi.hoisted(() => ({
	reload: vi.fn(async () => undefined),
	status: null as null | {
		aheadCount: number;
		behindCount: number;
		isHistoryPartial: boolean;
	},
}));

vi.mock("sonner", () => ({ toast }));
vi.mock("@/lib/commands", () => ({ sendRequestWithResponse: vi.fn() }));
vi.mock("@/lib/settings/settingsStore", () => ({
	setSetting: vi.fn(),
	useSetting: () => "Fetch",
}));
vi.mock("./useRemoteSyncStatus", () => ({ useRemoteSyncStatus: () => sync }));

const send = vi.mocked(sendRequestWithResponse);

describe("RemoteActionsControl", () => {
	beforeEach(() => {
		send.mockReset();
		toast.error.mockReset();
		toast.loading.mockClear();
		toast.success.mockReset();
		sync.reload.mockClear();
		sync.status = null;
	});

	it("keeps normal push one click and disables remote controls while busy", async () => {
		const user = userEvent.setup();
		const pending = deferred<void>();
		send.mockReturnValueOnce(pending.promise);
		render(
			<RemoteActionsControl currentBranchName="main" repositoryId="repo-1" />,
		);

		await user.click(screen.getByRole("button", { name: "Push" }));
		expect(send).toHaveBeenCalledWith(
			{
				commandType: "PushRepository",
				arguments: {
					pullMode: "Merge",
					pushMode: "Normal",
					remoteName: null,
					repositoryId: "repo-1",
				},
			},
			{ timeoutMs: 120_000 },
		);
		expect(
			screen.getByRole("button", { name: "More push actions" }),
		).toBeDisabled();
		expect(
			screen.getByRole("button", { name: "Manage remotes" }),
		).toBeDisabled();

		pending.resolve();
		await waitFor(() =>
			expect(screen.getByRole("button", { name: "Push" })).toBeEnabled(),
		);
		expect(toast.success).toHaveBeenCalledWith("Push complete", {
			id: "toast-1",
		});
		expect(sync.reload).toHaveBeenCalledOnce();
	});

	it("confirms force-with-lease, keeps the dialog retryable on failure, then closes", async () => {
		const user = userEvent.setup();
		send
			.mockRejectedValueOnce(new Error("remote changed since your last fetch"))
			.mockResolvedValueOnce(undefined);
		render(
			<RemoteActionsControl
				currentBranchName="feature/rewrite"
				repositoryId="repo-1"
			/>,
		);

		await user.click(screen.getByRole("button", { name: "More push actions" }));
		await user.click(await screen.findByText("Force push with lease…"));
		expect(
			await screen.findByRole(
				"heading",
				{ name: "Force push feature/rewrite?" },
				{ timeout: 5_000 },
			),
		).toBeInTheDocument();
		expect(screen.getByText(/protected by the lease/)).toBeInTheDocument();
		expect(send).not.toHaveBeenCalled();

		await user.click(
			screen.getByRole("button", { name: "Force push with lease" }),
		);
		await waitFor(() =>
			expect(toast.error).toHaveBeenCalledWith(
				"remote changed since your last fetch",
				{ id: "toast-1" },
			),
		);
		expect(
			screen.getByRole("heading", { name: /Force push feature\/rewrite/ }),
		).toBeInTheDocument();

		await user.click(
			screen.getByRole("button", { name: "Force push with lease" }),
		);
		await waitFor(() =>
			expect(
				screen.queryByRole("heading", { name: /Force push feature\/rewrite/ }),
			).not.toBeInTheDocument(),
		);
		expect(send).toHaveBeenLastCalledWith(
			{
				commandType: "PushRepository",
				arguments: {
					pullMode: "Merge",
					pushMode: "ForceWithLease",
					remoteName: null,
					repositoryId: "repo-1",
				},
			},
			{ timeoutMs: 120_000 },
		);
	});

	it("disables every remote mutation when no repository is selected", () => {
		render(
			<RemoteActionsControl currentBranchName={null} repositoryId={null} />,
		);

		expect(screen.getByRole("button", { name: "Fetch all" })).toBeDisabled();
		expect(screen.getByRole("button", { name: "Push" })).toBeDisabled();
		expect(
			screen.getByRole("button", { name: "More push actions" }),
		).toBeDisabled();
	});

	it("adds accessible animated incoming and outgoing counts", () => {
		sync.status = { aheadCount: 2, behindCount: 1, isHistoryPartial: false };
		render(
			<RemoteActionsControl currentBranchName="main" repositoryId="repo-1" />,
		);

		expect(
			screen.getByRole("button", { name: "Fetch all, 1 incoming commit" }),
		).toBeEnabled();
		expect(
			screen.getByRole("button", { name: "Push, 2 outgoing commits" }),
		).toBeEnabled();
		expect(screen.getByText("1")).toBeInTheDocument();
		expect(screen.getByText("2")).toBeInTheDocument();
	});
});

function deferred<T>() {
	let resolve!: (value: T | PromiseLike<T>) => void;
	let reject!: (reason?: unknown) => void;
	const promise = new Promise<T>((resolvePromise, rejectPromise) => {
		resolve = resolvePromise;
		reject = rejectPromise;
	});
	return { promise, reject, resolve };
}
