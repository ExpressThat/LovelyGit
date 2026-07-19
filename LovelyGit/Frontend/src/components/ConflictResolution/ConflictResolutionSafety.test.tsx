// @vitest-environment jsdom

import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { sendRequestWithResponse } from "@/lib/commands";
import { gitMutationTimeoutMs } from "@/lib/gitMutationTimeout";
import { ConflictResolutionView } from "./ConflictResolutionView";
import {
	conflictFile,
	renderConflictView,
	response,
} from "./ConflictResolutionViewTestFixtures";
import { verifyExternalConflictResolved } from "./externalConflictVerification";

vi.mock("@/lib/commands", () => ({ sendRequestWithResponse: vi.fn() }));
vi.mock("./externalConflictVerification", () => ({
	verifyExternalConflictResolved: vi.fn(),
}));
vi.mock("sonner", () => ({ toast: { success: vi.fn() } }));
const settings = vi.hoisted(() => ({
	CommitDiffViewMode: "SideBySide",
	CommitDiffContextLines: 8,
	CommitDiffLineDisplayMode: "Changes",
	CommitDiffWrapLines: false,
	CommitDiffIgnoreWhitespace: false,
}));
vi.mock("@/lib/settings/settingsStore", () => ({
	setSetting: vi.fn(),
	useSetting: (name: keyof typeof settings) => settings[name],
}));

const send = vi.mocked(sendRequestWithResponse);
const verifyExternal = vi.mocked(verifyExternalConflictResolved);

describe("ConflictResolutionView safety states", () => {
	beforeEach(() => {
		send.mockReset();
		verifyExternal.mockReset();
		verifyExternal.mockResolvedValue(undefined);
		settings.CommitDiffIgnoreWhitespace = false;
	});

	it("requires explicit omit and lets reset return it to unresolved base content", async () => {
		const user = userEvent.setup();
		send.mockResolvedValueOnce(response());
		renderConflictView(vi.fn(), vi.fn());
		const output = await screen.findByLabelText("Editable result preview");

		await user.click(screen.getByRole("button", { name: "Omit conflict" }));
		expect(output).toHaveValue("before\nafter\n");
		expect(screen.getByRole("button", { name: "Save & stage" })).toBeEnabled();
		await user.click(screen.getByRole("button", { name: "Reset" }));
		expect(output).toHaveValue("before\nbase\nafter\n");
		expect(screen.getByRole("button", { name: "Save & stage" })).toBeDisabled();
	});

	it("does not allow a manual result containing conflict markers to be saved", async () => {
		const user = userEvent.setup();
		send.mockResolvedValueOnce(response());
		renderConflictView(vi.fn(), vi.fn());
		const output = await screen.findByLabelText("Editable result preview");
		await user.clear(output);
		await user.type(output, "<<<<<<< unresolved");

		expect(screen.getByRole("button", { name: "Save & stage" })).toBeDisabled();
		expect(screen.getByText("Resolution required")).toBeInTheDocument();
	});

	it("disables merge controls while the external tool is still open", async () => {
		const user = userEvent.setup();
		let rejectTool: ((error: Error) => void) | undefined;
		const pendingTool = new Promise<undefined>((_, reject) => {
			rejectTool = reject;
		});
		send.mockResolvedValueOnce(response()).mockReturnValueOnce(pendingTool);
		renderConflictView(vi.fn(), vi.fn());
		await screen.findByLabelText("Editable result preview");
		await user.click(
			screen.getByRole("button", { name: "Open external merge tool" }),
		);

		expect(
			screen.getByRole("button", { name: "Merge tool open…" }),
		).toBeDisabled();
		expect(screen.getByRole("button", { name: "Save & stage" })).toBeDisabled();
		expect(
			screen.getByRole("button", { name: "Close conflict resolver" }),
		).toBeDisabled();
		expect(
			screen.getByRole("checkbox", { name: "Keep entire current chunk 1" }),
		).toHaveAttribute("aria-disabled", "true");
		rejectTool?.(new Error("cancelled"));
		expect(await screen.findByText("cancelled")).toBeInTheDocument();
	});

	it("keeps the draft open when an external tool reports success but leaves the file unmerged", async () => {
		const user = userEvent.setup();
		const onChange = vi.fn();
		const onClose = vi.fn();
		send.mockResolvedValueOnce(response()).mockResolvedValueOnce(undefined);
		verifyExternal.mockRejectedValueOnce(
			new Error(
				"The external merge tool closed without resolving this conflict.",
			),
		);
		renderConflictView(onChange, onClose);

		await user.click(
			await screen.findByRole("button", {
				name: "Open external merge tool",
			}),
		);

		expect(
			await screen.findByText(/closed without resolving this conflict/i),
		).toBeInTheDocument();
		expect(onChange).not.toHaveBeenCalled();
		expect(onClose).not.toHaveBeenCalled();
		expect(screen.getByLabelText("Editable result preview")).toHaveValue(
			"before\nbase\nafter\n",
		);
		expect(
			screen.getByRole("button", { name: "Open external merge tool" }),
		).toBeEnabled();
	});

	it("keeps the structured draft when comparison settings reload the same conflict", async () => {
		const user = userEvent.setup();
		send.mockResolvedValueOnce(response()).mockResolvedValueOnce(response());
		const { rerender } = renderConflictView(vi.fn(), vi.fn());
		await user.click(
			await screen.findByRole("checkbox", {
				name: "Keep entire current chunk 1",
			}),
		);
		expect(screen.getByLabelText("Editable result preview")).toHaveValue(
			"before\ncurrent\nafter\n",
		);

		settings.CommitDiffIgnoreWhitespace = true;
		rerender(
			<ConflictResolutionView
				file={conflictFile()}
				onChange={vi.fn()}
				onClose={vi.fn()}
				repositoryId="repo-1"
			/>,
		);
		expect(await screen.findByLabelText("Editable result preview")).toHaveValue(
			"before\ncurrent\nafter\n",
		);
	});

	it("uses a whole incoming file for binary conflicts", async () => {
		const user = userEvent.setup();
		const binary = response();
		binary.currentComparison = null;
		binary.incomingComparison = null;
		binary.ours = { ...binary.ours, isBinary: true, text: null };
		binary.theirs = { ...binary.theirs, isBinary: true, text: null };
		binary.result = { ...binary.result, isBinary: true, text: null };
		send.mockResolvedValueOnce(binary).mockResolvedValueOnce(undefined);
		renderConflictView(vi.fn(), vi.fn());

		await user.click(
			await screen.findByRole("button", { name: "Use incoming branch" }),
		);
		await user.click(screen.getByRole("button", { name: "Save & stage" }));
		await waitFor(() =>
			expect(send).toHaveBeenLastCalledWith(
				expect.objectContaining({
					commandType: "ResolveConflict",
					arguments: expect.objectContaining({
						resultText: null,
						resultTextGzipBase64: "",
						source: "Theirs",
						deleteResult: false,
					}),
				}),
				{ timeoutMs: gitMutationTimeoutMs },
			),
		);
	});
});
