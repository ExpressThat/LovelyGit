import { describe, expect, it } from "vitest";
import { normalizeCommitMessage } from "./WorkingChangesPanelCommands";

describe("normalizeCommitMessage", () => {
	it("trims the title and body sent to git", () => {
		expect(
			normalizeCommitMessage(
				"  Add working tree feedback  ",
				"\nBody text\n\n",
			),
		).toEqual({
			body: "Body text",
			title: "Add working tree feedback",
		});
	});

	it("keeps an empty body empty after trimming", () => {
		expect(normalizeCommitMessage(" Commit title ", "   ")).toEqual({
			body: "",
			title: "Commit title",
		});
	});
});
