// @vitest-environment jsdom

import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { sendRequestWithResponse } from "@/lib/commands";
import {
	renderConflictView,
	response,
} from "./ConflictResolutionViewTestFixtures";

const setSetting = vi.hoisted(() => vi.fn());
vi.mock("@/lib/commands", () => ({ sendRequestWithResponse: vi.fn() }));
vi.mock("sonner", () => ({ toast: { success: vi.fn() } }));
vi.mock("@/lib/settings/settingsStore", () => ({
	setSetting,
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

describe("ConflictResolutionView display controls", () => {
	beforeEach(() => {
		send.mockReset();
		setSetting.mockReset();
	});

	it("requests the selected comparison and exposes a scrollable toolbar", async () => {
		send.mockResolvedValueOnce(response());
		renderConflictView(vi.fn(), vi.fn());

		await screen.findByLabelText("Editable result preview");
		expect(send).toHaveBeenCalledWith({
			commandType: "GetConflictResolution",
			arguments: {
				repositoryId: "repo-1",
				path: "src/file.txt",
				viewMode: "SideBySide",
				ignoreWhitespace: false,
			},
		});
		expect(
			screen.getByRole("toolbar", { name: "Diff display controls" }),
		).toHaveClass("overflow-x-auto");
	});

	it("routes full-file, wrapping, and whitespace controls to settings", async () => {
		const user = userEvent.setup();
		send.mockResolvedValueOnce(response());
		renderConflictView(vi.fn(), vi.fn());
		await screen.findByLabelText("Editable result preview");

		await user.click(screen.getByRole("button", { name: "Full file" }));
		await user.click(screen.getByRole("button", { name: "Wrap lines" }));
		await user.click(screen.getByRole("button", { name: "Ignore whitespace" }));

		expect(setSetting).toHaveBeenNthCalledWith(
			1,
			"CommitDiffLineDisplayMode",
			"FullFile",
		);
		expect(setSetting).toHaveBeenNthCalledWith(2, "CommitDiffWrapLines", true);
		expect(setSetting).toHaveBeenNthCalledWith(
			3,
			"CommitDiffIgnoreWhitespace",
			true,
		);
	});
});
