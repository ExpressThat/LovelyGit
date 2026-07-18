import { describe, expect, it } from "vitest";
import type { FileBlameResponse } from "@/generated/types";
import { encodeGzipBase64 } from "../CommitFileDiff/compactPayloadCompression";
import { expandFileBlamePayload } from "./fileBlamePayload";

describe("expandFileBlamePayload", () => {
	it("leaves ordinary responses unchanged", async () => {
		const response = blameResponse("one\n");

		expect(await expandFileBlamePayload(response)).toBe(response);
	});

	it("restores a complete compact response", async () => {
		const original = blameResponse("one\ntwo\n");
		const compact = blameResponse("");
		compact.hunks = [];
		compact.compactPayloadGzipBase64 = await encodeGzipBase64(
			JSON.stringify(original),
		);

		expect(await expandFileBlamePayload(compact)).toEqual(original);
	});

	it("rejects ambiguous compact and readable content", async () => {
		const response = blameResponse("one\n");
		response.compactPayloadGzipBase64 = "compressed";

		await expect(expandFileBlamePayload(response)).rejects.toThrow("ambiguous");
	});
});

function blameResponse(content: string): FileBlameResponse {
	return {
		compactPayloadGzipBase64: "",
		content,
		hunks: [
			{
				author: "Ross",
				date: 1,
				email: "r@example.test",
				hash: "abc",
				lineCount: 2,
				startLine: 1,
				subject: "Change",
			},
		],
		isPartial: false,
		lineCount: 2,
		path: "large.txt",
		resolvedLineCount: 2,
		scannedCommitCount: 2,
		startCommitHash: "abc",
	};
}
