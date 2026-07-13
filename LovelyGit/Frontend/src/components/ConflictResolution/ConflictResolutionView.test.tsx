// @vitest-environment jsdom

import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { sendRequestWithResponse } from "@/lib/commands";
import { gitMutationTimeoutMs } from "@/lib/gitMutationTimeout";
import {
	renderConflictView,
	response,
} from "./ConflictResolutionViewTestFixtures";

vi.mock("@/lib/commands", () => ({ sendRequestWithResponse: vi.fn() }));
vi.mock("sonner", () => ({ toast: { success: vi.fn() } }));
vi.mock("@/lib/settings/settingsStore", () => ({
	setSetting: vi.fn(),
	useSetting: (name: string) =>
		({
			CommitDiffViewMode: "SideBySide",
			CommitDiffContextLines: 3,
			CommitDiffLineDisplayMode: "Changes",
			CommitDiffWrapLines: false,
			CommitDiffIgnoreWhitespace: false,
		})[name],
}));

const send = vi.mocked(sendRequestWithResponse);

describe("ConflictResolutionView", () => {
	beforeEach(() => {
		send.mockReset();
	});

	it("shows marker-free base content but keeps the untouched conflict unresolved", async () => {
		send.mockResolvedValueOnce(response());
		renderConflictView(vi.fn(), vi.fn());

		expect(await screen.findByLabelText("Editable result preview")).toHaveValue(
			"before\nbase\nafter\n",
		);
		expect(screen.getByRole("button", { name: "Save & stage" })).toBeDisabled();
		expect(screen.queryByText("<<<<<<< HEAD")).not.toBeInTheDocument();
	});

	it("renders visible result line numbers in one lightweight text node", async () => {
		send.mockResolvedValueOnce(response());
		renderConflictView(vi.fn(), vi.fn());

		await screen.findByLabelText("Editable result preview");
		const gutter = screen.getByTestId("conflict-result-line-numbers");
		expect(gutter).toHaveTextContent("1 2 3 4");
		expect(gutter.childElementCount).toBe(0);
	});

	it("selects a current chunk and saves the exact result", async () => {
		const user = userEvent.setup();
		const onChange = vi.fn();
		const onClose = vi.fn();
		send.mockResolvedValueOnce(response()).mockResolvedValueOnce(undefined);
		renderConflictView(onChange, onClose);

		await user.click(
			await screen.findByRole("checkbox", {
				name: "Keep entire current chunk 1",
			}),
		);
		expect(screen.getByLabelText("Editable result preview")).toHaveValue(
			"before\ncurrent\nafter\n",
		);
		await user.click(screen.getByRole("button", { name: "Save & stage" }));

		await waitFor(() =>
			expect(send).toHaveBeenLastCalledWith(
				{
					commandType: "ResolveConflict",
					arguments: {
						repositoryId: "repo-1",
						path: "src/file.txt",
						expectedFingerprint: "ABC123",
						resultText: "before\ncurrent\nafter\n",
						source: null,
						deleteResult: false,
					},
				},
				{ timeoutMs: gitMutationTimeoutMs },
			),
		);
		expect(onChange).toHaveBeenCalledOnce();
		expect(onClose).toHaveBeenCalledOnce();
	});

	it("combines selected source lines in stable current then incoming order", async () => {
		const user = userEvent.setup();
		send.mockResolvedValueOnce(
			response({
				current: "before\ncurrent one\ncurrent two\nafter\n",
				incoming: "before\nincoming one\nincoming two\nafter\n",
				result:
					"before\n<<<<<<< HEAD\ncurrent one\ncurrent two\n=======\nincoming one\nincoming two\n>>>>>>> feature\nafter\n",
			}),
		);
		renderConflictView(vi.fn(), vi.fn());

		await user.click(
			await screen.findByRole("button", { name: "Apply incoming line 2" }),
		);
		await user.click(
			screen.getByRole("button", { name: "Apply current line 3" }),
		);

		expect(
			screen.getByRole("checkbox", { name: "Keep entire current chunk 1" }),
		).toHaveAttribute("aria-checked", "mixed");
		expect(screen.getByLabelText("Editable result preview")).toHaveValue(
			"before\ncurrent two\nincoming one\nafter\n",
		);
	});

	it("keeps deleted-base rows as red indicators without an apply action", async () => {
		send.mockResolvedValueOnce(response());
		renderConflictView(vi.fn(), vi.fn());

		const baseRows = await screen.findAllByText("base");
		const redRow = baseRows
			.map((node) => node.closest(".grid"))
			.find((node) => node?.className.includes("bg-red-500/8"));
		expect(redRow).toBeTruthy();
		expect(
			screen.queryByRole("button", { name: /Apply current line 0/ }),
		).not.toBeInTheDocument();
	});

	it("supports an empty deletion candidate and explicit omit", async () => {
		const user = userEvent.setup();
		send.mockResolvedValueOnce(
			response({
				current: "before\nafter\n",
				result:
					"before\n<<<<<<< HEAD\n||||||| base\nbase\n=======\nincoming\n>>>>>>> feature\nafter\n",
			}),
		);
		renderConflictView(vi.fn(), vi.fn());

		await user.click(
			await screen.findByRole("checkbox", {
				name: "Keep entire current chunk 1",
			}),
		);
		expect(screen.getByLabelText("Editable result preview")).toHaveValue(
			"before\nafter\n",
		);
		expect(screen.getByRole("button", { name: "Save & stage" })).toBeEnabled();
	});

	it("manual edits lock source actions until reset and never retain markers", async () => {
		const user = userEvent.setup();
		send.mockResolvedValueOnce(response());
		renderConflictView(vi.fn(), vi.fn());
		const output = await screen.findByLabelText("Editable result preview");
		await user.clear(output);
		await user.type(output, "custom resolution\n");

		expect(
			screen.getByRole("checkbox", { name: "Keep entire current chunk 1" }),
		).toHaveAttribute("aria-disabled", "true");
		expect(screen.getByText(/Manual editing is active/)).toBeInTheDocument();
		expect(screen.getByRole("button", { name: "Save & stage" })).toBeEnabled();
		await user.click(screen.getByRole("button", { name: "Reset" }));
		expect(output).toHaveValue("before\nbase\nafter\n");
		expect(
			screen.getByRole("checkbox", { name: "Keep entire current chunk 1" }),
		).not.toHaveAttribute("aria-disabled", "true");
	});

	it("surfaces save failure and allows a successful retry", async () => {
		const user = userEvent.setup();
		const onClose = vi.fn();
		send
			.mockResolvedValueOnce(response())
			.mockRejectedValueOnce(new Error("index is locked"))
			.mockResolvedValueOnce(undefined);
		renderConflictView(vi.fn(), onClose);
		await user.click(
			await screen.findByRole("checkbox", {
				name: "Keep entire incoming chunk 1",
			}),
		);
		await user.click(screen.getByRole("button", { name: "Save & stage" }));
		expect(await screen.findByText("index is locked")).toBeInTheDocument();
		expect(onClose).not.toHaveBeenCalled();
		await user.click(screen.getByRole("button", { name: "Save & stage" }));
		await waitFor(() => expect(onClose).toHaveBeenCalledOnce());
	});

	it("preserves the draft and re-enables controls when an external tool fails", async () => {
		const user = userEvent.setup();
		send
			.mockResolvedValueOnce(response())
			.mockRejectedValueOnce(new Error("tool unavailable"));
		renderConflictView(vi.fn(), vi.fn());
		const output = await screen.findByLabelText("Editable result preview");
		await user.click(
			screen.getByRole("button", { name: "Open external merge tool" }),
		);

		expect(await screen.findByText("tool unavailable")).toBeInTheDocument();
		expect(output).toHaveValue("before\nbase\nafter\n");
		expect(
			screen.getByRole("button", { name: "Open external merge tool" }),
		).toBeEnabled();
		expect(send).toHaveBeenLastCalledWith(
			{
				commandType: "OpenConflictInMergeTool",
				arguments: { repositoryId: "repo-1", path: "src/file.txt" },
			},
			{ timeoutMs: gitMutationTimeoutMs },
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
						source: "Theirs",
						deleteResult: false,
					}),
				}),
				{ timeoutMs: gitMutationTimeoutMs },
			),
		);
	});
});
