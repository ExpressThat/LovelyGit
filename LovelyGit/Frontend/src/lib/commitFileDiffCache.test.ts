import { beforeEach, describe, expect, it } from "vitest";
import type { CommitFileDiffResponse } from "@/generated/types";
import {
	cacheCommitFileDiff,
	cacheCommitFileDiffViews,
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
	newLineEnding: null,
	newLineEndingOverrides: [],
	oldLineEnding: null,
	oldLineEndingOverrides: [],
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

	it("shares canonical reference payloads with the alternate view mode", () => {
		const cached = {
			...response("large.txt"),
			compactLineSchema: "tuple-v4-delta-refs:gzip-base64:utf-8",
			compactSourceBundleGzipBase64: "sources",
		};
		const combinedKey = key({ viewMode: "Combined" });
		const sideBySideKey = key({ viewMode: "SideBySide" });

		cacheCommitFileDiffViews(combinedKey, sideBySideKey, cached);

		expect(getCachedCommitFileDiff(combinedKey)).toBe(cached);
		expect(getCachedCommitFileDiff(sideBySideKey)?.viewMode).toBe("SideBySide");
	});

	it("does not share view-specific rendered payloads", () => {
		const combinedKey = key({ viewMode: "Combined" });
		const sideBySideKey = key({ viewMode: "SideBySide" });

		cacheCommitFileDiffViews(combinedKey, sideBySideKey, response("small.txt"));

		expect(getCachedCommitFileDiff(sideBySideKey)).toBeUndefined();
	});

	it("retains both views only for the latest oversized payload", () => {
		const first = {
			...response("first-large.txt"),
			compactLineCount: 5_001,
			compactLineSchema: "tuple-v4-delta-refs:gzip-base64:utf-8",
			compactSourceBundleGzipBase64: "first-sources",
		};
		const second = {
			...response("second-large.txt"),
			compactLineCount: 5_001,
			compactLineSchema: "tuple-v4-delta-refs:gzip-base64:utf-8",
			compactSourceBundleGzipBase64: "second-sources",
		};

		cacheCommitFileDiffViews("first-combined", "first-side", first);
		cacheCommitFileDiffViews("second-combined", "second-side", second);

		expect(getCachedCommitFileDiff("first-combined")).toBeUndefined();
		expect(getCachedCommitFileDiff("first-side")).toBeUndefined();
		expect(getCachedCommitFileDiff("second-combined")).toBe(second);
		expect(getCachedCommitFileDiff("second-side")?.viewMode).toBe("SideBySide");
	});

	it("evicts medium diffs when their combined decoded weight exceeds the budget", () => {
		for (let index = 0; index < 5; index += 1) {
			cacheCommitFileDiff(`medium-${index}`, {
				...response(`${index}.txt`),
				compactLineCount: 5_000,
				compactLinesGzipBase64: "x",
			});
		}

		expect(getCachedCommitFileDiff("medium-0")).toBeUndefined();
		for (let index = 1; index < 5; index += 1) {
			expect(getCachedCommitFileDiff(`medium-${index}`)).toBeDefined();
		}
	});

	it("keeps both layouts when one active payload exceeds the total budget", () => {
		const huge = {
			...response("huge.txt"),
			compactLineCount: 30_000,
			compactLineSchema: "tuple-v4-delta-refs:gzip-base64:utf-8",
			compactSourceBundleGzipBase64: "sources",
		};

		cacheCommitFileDiffViews("huge-combined", "huge-side", huge);

		expect(getCachedCommitFileDiff("huge-combined")).toBe(huge);
		expect(getCachedCommitFileDiff("huge-side")?.viewMode).toBe("SideBySide");
	});

	it("releases older normal payloads when a huge diff becomes active", () => {
		for (let index = 0; index < 4; index += 1) {
			cacheCommitFileDiff(`normal-${index}`, response(`${index}.txt`));
		}
		cacheCommitFileDiffViews("huge-combined", "huge-side", {
			...response("huge.txt"),
			compactLineCount: 30_000,
			compactLineSchema: "tuple-v4-delta-refs:gzip-base64:utf-8",
			compactSourceBundleGzipBase64: "sources",
		});

		for (let index = 0; index < 4; index += 1) {
			expect(getCachedCommitFileDiff(`normal-${index}`)).toBeUndefined();
		}
		expect(getCachedCommitFileDiff("huge-combined")).toBeDefined();
		expect(getCachedCommitFileDiff("huge-side")).toBeDefined();
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
