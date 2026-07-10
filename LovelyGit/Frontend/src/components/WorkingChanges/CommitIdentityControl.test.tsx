// @vitest-environment jsdom

import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { GitCommitIdentity } from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { NativeMessageType } from "@/lib/nativeMessaging";
import { CommitIdentityControl } from "./CommitIdentityControl";

vi.mock("@/lib/commands", () => ({
	sendRequestWithResponse: vi.fn(),
}));

const send = vi.mocked(sendRequestWithResponse);

describe("CommitIdentityControl", () => {
	beforeEach(() => send.mockReset());

	it("shows the effective native identity and its source", async () => {
		send.mockResolvedValueOnce(globalIdentity());

		render(<CommitIdentityControl disabled={false} repositoryId="repo-1" />);

		expect(
			await screen.findByText("Ada Lovelace <ada@example.test>"),
		).toBeInTheDocument();
		expect(screen.getByText("Git defaults")).toBeInTheDocument();
		expect(send).toHaveBeenCalledWith({
			commandType: NativeMessageType.GetCommitIdentity,
			arguments: { repositoryId: "repo-1" },
		});
	});

	it("saves a repository-local override and updates the summary", async () => {
		const user = userEvent.setup();
		send
			.mockResolvedValueOnce(globalIdentity())
			.mockResolvedValueOnce(repositoryIdentity());
		render(<CommitIdentityControl disabled={false} repositoryId="repo-1" />);
		await screen.findByText("Ada Lovelace <ada@example.test>");

		await user.click(
			screen.getByRole("button", { name: "Edit commit identity" }),
		);
		const name = await screen.findByLabelText("Name", {}, { timeout: 5_000 });
		await user.clear(name);
		await user.type(name, "Grace Hopper");
		const email = screen.getByLabelText("Email");
		await user.clear(email);
		await user.type(email, "grace@example.test");
		await user.click(
			screen.getByRole("button", { name: "Save for this repository" }),
		);

		await waitFor(() =>
			expect(send).toHaveBeenLastCalledWith({
				commandType: NativeMessageType.ManageCommitIdentity,
				arguments: {
					clearRepositoryOverride: false,
					email: "grace@example.test",
					name: "Grace Hopper",
					repositoryId: "repo-1",
				},
			}),
		);
		expect(
			await screen.findByText("Grace Hopper <grace@example.test>"),
		).toBeInTheDocument();
		expect(screen.getByText("This repository")).toBeInTheDocument();
	}, 15_000);

	it("clears a local override and returns to Git defaults", async () => {
		const user = userEvent.setup();
		send
			.mockResolvedValueOnce(repositoryIdentity())
			.mockResolvedValueOnce(globalIdentity());
		render(<CommitIdentityControl disabled={false} repositoryId="repo-1" />);
		await screen.findByText("Grace Hopper <grace@example.test>");

		await user.click(
			screen.getByRole("button", { name: "Edit commit identity" }),
		);
		await user.click(
			await screen.findByRole(
				"button",
				{ name: "Use Git defaults" },
				{ timeout: 5_000 },
			),
		);

		await waitFor(() =>
			expect(send).toHaveBeenLastCalledWith({
				commandType: NativeMessageType.ManageCommitIdentity,
				arguments: {
					clearRepositoryOverride: true,
					email: null,
					name: null,
					repositoryId: "repo-1",
				},
			}),
		);
		expect(await screen.findByText("Git defaults")).toBeInTheDocument();
	}, 15_000);

	it("makes a missing identity actionable", async () => {
		send.mockResolvedValueOnce({
			email: null,
			emailSource: "Missing",
			hasRepositoryOverride: false,
			isComplete: false,
			name: null,
			nameSource: "Missing",
		});

		render(<CommitIdentityControl disabled={false} repositoryId="repo-1" />);

		expect(
			await screen.findByText("Commit identity not configured"),
		).toBeInTheDocument();
		expect(
			screen.getByText("Add an author name and email before committing"),
		).toBeInTheDocument();
	});

	it("shows a read failure without trapping the user in loading state", async () => {
		send.mockRejectedValueOnce(new Error("Git configuration is unreadable"));

		render(<CommitIdentityControl disabled={false} repositoryId="repo-1" />);

		expect(
			await screen.findByText("Commit identity unavailable"),
		).toBeInTheDocument();
		await waitFor(() =>
			expect(screen.getByText("Git configuration is unreadable")).toBeVisible(),
		);
		expect(
			screen.getByRole("button", { name: "Edit commit identity" }),
		).toBeEnabled();
	});

	it("keeps failed edits open and permits retry", async () => {
		const user = userEvent.setup();
		send.mockResolvedValueOnce(globalIdentity());
		render(<CommitIdentityControl disabled={false} repositoryId="repo-1" />);
		await screen.findByText("Ada Lovelace <ada@example.test>");
		await user.click(
			screen.getByRole("button", { name: "Edit commit identity" }),
		);
		const name = await screen.findByLabelText("Name", {}, { timeout: 5_000 });
		await user.clear(name);
		await user.type(name, "Grace Hopper");

		send.mockRejectedValueOnce(new Error("Could not write config"));
		await user.click(
			screen.getByRole("button", { name: "Save for this repository" }),
		);
		expect(await screen.findByText("Could not write config")).toBeVisible();
		expect(screen.getByRole("dialog")).toBeVisible();

		send.mockResolvedValueOnce(repositoryIdentity());
		await user.click(
			screen.getByRole("button", { name: "Save for this repository" }),
		);
		await waitFor(() =>
			expect(
				screen.getByText("Grace Hopper <grace@example.test>"),
			).toBeVisible(),
		);
		expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
	}, 15_000);
});

function globalIdentity(): GitCommitIdentity {
	return {
		email: "ada@example.test",
		emailSource: "Global",
		hasRepositoryOverride: false,
		isComplete: true,
		name: "Ada Lovelace",
		nameSource: "Global",
	};
}

function repositoryIdentity(): GitCommitIdentity {
	return {
		email: "grace@example.test",
		emailSource: "Repository",
		hasRepositoryOverride: true,
		isComplete: true,
		name: "Grace Hopper",
		nameSource: "Repository",
	};
}
