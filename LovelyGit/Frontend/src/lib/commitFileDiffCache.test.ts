import { beforeEach, describe, expect, it } from "vitest";
import type { CommitFileDiffResponse } from "@/generated/types";
import {
	cacheCommitFileDiff,
	clearCommitFileDiffCache,
	commitFileDiffCacheKey,
	getCachedCommitFileDiff,
} from "./commitFileDiffCache";

const response = (path: string): CommitFileDiffResponse => ({
	commitHash: "commit",
	compactLineCount: 0,
	compactLineSchema: "",
	compactLinesGzipBase64: "",
	compactSourceSchema: "",
	compactSourceBundleGzipBase64: "",
	hasDifferences: true,
	isBinary: false,
	isTruncated: false,
	lines: [],
	path,
	status: "Modified",
	truncationMessage: "",
	virtualChangeType: "",
	virtualLineCount: 0,
	virtualText: "",
	virtualTextEncoding: "",
	virtualTextGzipBase64: "",
	viewMode: "Combined",
});

const key = (overrides: Record<string, unknown> = {}) =>
	commitFileDiffCacheKey({
		commitHash: "commit",
		filePath: "file.txt",
		ignoreWhitespace: false,
		parentIndex: 0,
		repositoryId: "repo",
		viewMode: "Combined",
		...overrides,
	});

describe("commitFileDiffCache", () => {
	beforeEach(clearCommitFileDiffCache);

	it("isolates every input that changes diff content", () => {
		const baseline = key();
		for (const variant of [
			key({ repositoryId: "other" }),
			key({ commitHash: "other" }),
			key({ comparisonCommitHash: "base" }),
			key({ parentIndex: 1 }),
			key({ filePath: "other.txt" }),
			key({ viewMode: "SideBySide" }),
			key({ ignoreWhitespace: true }),
		]) {
			expect(variant).not.toBe(baseline);
		}
	});

	it("returns the cached response without transforming colored line data", () => {
		const cached = response("colored.txt");
		cacheCommitFileDiff(key(), cached);
		expect(getCachedCommitFileDiff(key())).toBe(cached);
	});

	it("evicts the least recently used response after eight variants", () => {
		for (let index = 0; index < 8; index += 1) {
			cacheCommitFileDiff(
				key({ filePath: `${index}.txt` }),
				response(`${index}`),
			);
		}
		getCachedCommitFileDiff(key({ filePath: "0.txt" }));
		cacheCommitFileDiff(key({ filePath: "8.txt" }), response("8"));

		expect(getCachedCommitFileDiff(key({ filePath: "0.txt" }))).toBeDefined();
		expect(getCachedCommitFileDiff(key({ filePath: "1.txt" }))).toBeUndefined();
	});
});
