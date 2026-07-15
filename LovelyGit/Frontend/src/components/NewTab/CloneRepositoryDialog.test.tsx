// @vitest-environment jsdom

import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { sendRequestWithResponse } from "@/lib/commands";
import { CloneRepositoryDialog } from "./CloneRepositoryDialog";

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

const send = vi.mocked(sendRequestWithResponse);

describe("CloneRepositoryDialog", () => {
	beforeEach(() => {
		vi.clearAllMocks();
		vi.spyOn(globalThis.crypto, "randomUUID").mockReturnValue(
			"11111111-1111-4111-8111-111111111111",
		);
	});
	afterEach(() => vi.restoreAllMocks());

	it("sends the folder name entered through a WebView input event", async () => {
		const user = userEvent.setup();
		send.mockResolvedValueOnce({
			id: "repository-id",
			name: "custom-name",
			path: "C:\\temp\\custom-name",
		});
		render(<CloneRepositoryDialog />);
		await user.click(screen.getByRole("button", { name: "Clone Repository" }));

		fireEvent.input(screen.getByLabelText("Repository URL"), {
			target: { value: "https://example.test/team/inferred.git" },
		});
		fireEvent.input(screen.getByLabelText("Destination folder"), {
			target: { value: "C:\\temp" },
		});
		fireEvent.input(screen.getByLabelText("Repository folder name"), {
			target: { value: "custom-name" },
		});
		await user.click(screen.getByRole("button", { name: "Clone and open" }));

		await waitFor(() => expect(send).toHaveBeenCalled());
		expect(send.mock.calls[0]?.[0]).toMatchObject({
			arguments: { directoryName: "custom-name" },
			commandType: "CloneRepository",
		});
	});
});
