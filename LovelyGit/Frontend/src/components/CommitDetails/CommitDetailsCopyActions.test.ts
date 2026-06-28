import { describe, expect, it } from "vitest";
import type { CommitDetailsResponse } from "@/generated/types";
import { buildCommitDetailsCopyActions } from "./CommitDetailsCopyActions";

describe("buildCommitDetailsCopyActions", () => {
	it("returns hash and subject actions for a subject-only commit", () => {
		const actions = buildCommitDetailsCopyActions(
			createDetails({
				message: "Add copy buttons",
				subject: "Add copy buttons",
			}),
		);

		expect(actions.map((action) => action.key)).toEqual([
			"hash",
			"shortHash",
			"subject",
		]);
		expect(actions[1]?.value).toBe("1234567");
	});

	it("adds the full message only when it has extra body text", () => {
		const actions = buildCommitDetailsCopyActions(
			createDetails({
				message: "Add copy buttons\n\nIncludes details.",
				subject: "Add copy buttons",
			}),
		);

		expect(actions.map((action) => action.key)).toContain("message");
		expect(actions.at(-1)?.value).toBe("Add copy buttons\n\nIncludes details.");
	});
});

function createDetails(
	overrides: Partial<CommitDetailsResponse>,
): CommitDetailsResponse {
	return {
		author: "Ross",
		body: "",
		branches: [],
		changedFiles: [],
		date: 0,
		email: "ross@example.com",
		hash: "1234567890abcdef",
		message: "",
		parents: [],
		stats: { additions: 0, deletions: 0 },
		subject: "",
		tags: [],
		...overrides,
	};
}
