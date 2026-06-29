import { describe, expect, it } from "vitest";
import { getChangedFileListHeight } from "./CommitDetailsChangedFilesList";

describe("getChangedFileListHeight", () => {
	it("grows with small changed-file lists", () => {
		expect(getChangedFileListHeight(3)).toBe(126);
	});

	it("caps tall changed-file lists so rows can virtualize", () => {
		expect(getChangedFileListHeight(57)).toBe(420);
	});
});
