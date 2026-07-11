// @vitest-environment jsdom

import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { KnownGitRepository } from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { RecentRepositoryRow } from "./RecentRepositoryRow";

const toastError = vi.hoisted(() => vi.fn());
vi.mock("@/lib/commands", () => ({ sendRequestWithResponse: vi.fn() }));
vi.mock("sonner", () => ({ toast: { error: toastError } }));

const send = vi.mocked(sendRequestWithResponse);

describe("RecentRepositoryRow", () => {
	beforeEach(() => {
		send.mockReset();
		toastError.mockReset();
	});

	it("opens the repository from the row and context menu", async () => {
		const user = userEvent.setup();
		const onOpen = vi.fn();
		renderRow({ onOpen });

		await user.click(screen.getByRole("button", { name: /Lovely fixture/ }));
		expect(onOpen).toHaveBeenCalledOnce();

		openMenu();
		await user.click(await screen.findByText("Open in LovelyGit"));
		expect(onOpen).toHaveBeenCalledTimes(2);
	});

	it("reveals the repository and surfaces native failure", async () => {
		const user = userEvent.setup();
		send
			.mockResolvedValueOnce({})
			.mockRejectedValueOnce(new Error("Explorer unavailable"));
		renderRow();

		openMenu();
		await user.click(await screen.findByText("Show in File Explorer"));
		expect(send).toHaveBeenCalledWith({
			commandType: "RevealKnownGitRepository",
			arguments: { knownRepositoryId: "repo-1" },
		});

		openMenu();
		await user.click(await screen.findByText("Show in File Explorer"));
		await waitFor(() =>
			expect(toastError).toHaveBeenCalledWith("Explorer unavailable"),
		);
	});

	it("requires confirmation and cancellation leaves the repository intact", async () => {
		const user = userEvent.setup();
		const onRemove = vi.fn();
		renderRow({ onRemove });

		await openRemoveDialog(user);
		expect(screen.getByText(/Files on disk are not changed/)).toBeVisible();
		await user.click(screen.getByRole("button", { name: "Keep repository" }));

		expect(onRemove).not.toHaveBeenCalled();
		expect(
			screen.queryByRole("button", { name: "Remove from LovelyGit" }),
		).not.toBeInTheDocument();
	});

	it("keeps the dialog open after failure and allows a successful retry", async () => {
		const user = userEvent.setup();
		const onRemove = vi
			.fn<() => Promise<void>>()
			.mockRejectedValueOnce(new Error("database is locked"))
			.mockResolvedValueOnce();
		renderRow({ onRemove });
		await openRemoveDialog(user);

		await user.click(
			screen.getByRole("button", { name: "Remove from LovelyGit" }),
		);
		expect(await screen.findByText("database is locked")).toBeVisible();
		expect(
			screen.getByText("Remove Lovely fixture from LovelyGit?"),
		).toBeVisible();

		await user.click(
			screen.getByRole("button", { name: "Remove from LovelyGit" }),
		);
		await waitFor(() => expect(onRemove).toHaveBeenCalledTimes(2));
		await waitFor(() =>
			expect(
				screen.queryByText("Remove Lovely fixture from LovelyGit?"),
			).not.toBeInTheDocument(),
		);
	});
});

function renderRow({
	onOpen = vi.fn(),
	onRemove = vi.fn().mockResolvedValue(undefined),
}: {
	onOpen?: () => void;
	onRemove?: () => Promise<void>;
} = {}) {
	return render(
		<RecentRepositoryRow
			onOpen={onOpen}
			onRemove={onRemove}
			repository={repository()}
		/>,
	);
}

function openMenu() {
	fireEvent.contextMenu(screen.getByRole("button", { name: /Lovely fixture/ }));
}

async function openRemoveDialog(user: ReturnType<typeof userEvent.setup>) {
	openMenu();
	await user.click(await screen.findByText("Remove from LovelyGit…"));
	await screen.findByText("Remove Lovely fixture from LovelyGit?");
}

function repository(): KnownGitRepository {
	return {
		id: "repo-1",
		name: "Lovely fixture",
		path: "C:\\fixtures\\lovely",
	};
}
