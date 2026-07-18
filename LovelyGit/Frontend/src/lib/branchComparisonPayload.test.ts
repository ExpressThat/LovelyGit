import { describe, expect, it } from "vitest";
import { encodeGzipBase64 } from "@/components/CommitFileDiff/compactPayloadCompression";
import type {
	BranchComparisonFile,
	BranchComparisonResponse,
} from "@/generated/types";
import {
	expandBranchComparisonPayload,
	maximumBranchComparisonFiles,
} from "./branchComparisonPayload";

describe("expandBranchComparisonPayload", () => {
	it("restores every compact file and clears the compressed copy", async () => {
		const files: BranchComparisonFile[] = [
			{ path: "src/large.ts", status: "Modified" },
			{ path: "src/new.ts", status: "Added" },
		];
		const response = await compactResponse(files);

		await expect(expandBranchComparisonPayload(response)).resolves.toEqual({
			...response,
			compactFilesGzipBase64: null,
			files,
		});
	});

	it("preserves ordinary responses by identity", async () => {
		const response = ordinaryResponse();

		await expect(expandBranchComparisonPayload(response)).resolves.toBe(
			response,
		);
	});

	it("rejects malformed or oversized native payloads", async () => {
		const malformed = await compactResponse([
			{ path: "valid", status: "Added" },
		]);
		malformed.compactFilesGzipBase64 = await encodeGzipBase64(
			JSON.stringify([{ path: 42, status: "Added" }]),
		);
		const oversized = await compactResponse(
			Array.from({ length: maximumBranchComparisonFiles + 1 }, () => ({
				path: "same",
				status: "Modified",
			})),
		);

		await expect(expandBranchComparisonPayload(malformed)).rejects.toThrow(
			"file list is invalid",
		);
		await expect(expandBranchComparisonPayload(oversized)).rejects.toThrow(
			"file list is invalid",
		);
	});
});

async function compactResponse(
	files: BranchComparisonFile[],
): Promise<BranchComparisonResponse> {
	return {
		...ordinaryResponse(),
		compactFilesGzipBase64: await encodeGzipBase64(JSON.stringify(files)),
		files: [],
	};
}

function ordinaryResponse(): BranchComparisonResponse {
	return {
		aheadCommits: [],
		aheadCount: 0,
		behindCommits: [],
		behindCount: 0,
		changedFileCount: 0,
		compactFilesGzipBase64: null,
		currentBranchName: "main",
		currentHash: "a".repeat(40),
		files: [],
		isFileListTruncated: false,
		isHistoryPartial: false,
		mergeBaseHash: null,
		targetBranchName: "feature",
		targetHash: "b".repeat(40),
	};
}
