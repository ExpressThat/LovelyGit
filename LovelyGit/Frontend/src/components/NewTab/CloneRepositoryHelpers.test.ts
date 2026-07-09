import { describe, expect, it } from "vitest";
import { inferCloneDirectoryName } from "./CloneRepositoryHelpers";

describe("inferCloneDirectoryName", () => {
	it.each([
		["https://github.com/openai/codex.git", "codex"],
		["git@github.com:openai/codex.git", "codex"],
		["file:///C:/Projects/LovelyGit/", "LovelyGit"],
		["https://example.com/team/my%20repo.git?ref=main", "my repo"],
	])("infers a useful folder from %s", (remoteUrl, expected) => {
		expect(inferCloneDirectoryName(remoteUrl)).toBe(expected);
	});

	it("returns an empty value until a useful URL is entered", () => {
		expect(inferCloneDirectoryName("  ")).toBe("");
	});
});
