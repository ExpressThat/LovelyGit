// @vitest-environment jsdom

import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { sendRequestWithResponse } from "@/lib/commands";
import { RemoteWebActionControl } from "./RemoteWebActionControl";
import { openRemoteWebResource } from "./RepositoryCommands";

const toast = vi.hoisted(() => ({ error: vi.fn(), success: vi.fn() }));

vi.mock("sonner", () => ({ toast }));
vi.mock("@/lib/commands", () => ({ sendRequestWithResponse: vi.fn() }));

describe("remote website commands", () => {
	beforeEach(() => vi.clearAllMocks());

	it("opens a typed remote resource", async () => {
		vi.mocked(sendRequestWithResponse).mockResolvedValueOnce({});

		await openRemoteWebResource("repo", "Commit", "abc123");

		expect(sendRequestWithResponse).toHaveBeenCalledWith({
			arguments: {
				knownRepositoryId: "repo",
				kind: "Commit",
				value: "abc123",
			},
			commandType: "OpenRemoteWebResource",
		});
	});

	it("surfaces launch failures and permits a later retry", async () => {
		vi.mocked(sendRequestWithResponse)
			.mockRejectedValueOnce(new Error("No supported remote"))
			.mockResolvedValueOnce({});

		await openRemoteWebResource("repo", "Repository");
		await openRemoteWebResource("repo", "Repository");

		expect(toast.error).toHaveBeenCalledWith("No supported remote");
		expect(sendRequestWithResponse).toHaveBeenCalledTimes(2);
	});

	it("disables the toolbar action without a repository", async () => {
		const user = userEvent.setup();
		const { rerender } = render(<RemoteWebActionControl repositoryId={null} />);
		expect(screen.getByRole("button")).toBeDisabled();

		rerender(<RemoteWebActionControl repositoryId="repo" />);
		await user.click(screen.getByRole("button"));

		expect(sendRequestWithResponse).toHaveBeenCalledWith(
			expect.objectContaining({ commandType: "OpenRemoteWebResource" }),
		);
	});
});
