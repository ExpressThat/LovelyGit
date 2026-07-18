import { describe, expect, it } from "vitest";
import type { CommitFileDiffResponse } from "@/generated/types";
import { loadCompactLines, releaseCompactLines } from "./compactLinePayload";

describe("compact line payload lifecycle", () => {
	it("releases decoded rows for an unmounted oversized response", async () => {
		const diff = {
			compactLineCount: 10_000,
			compactLineSchema: "tuple-v2:gzip-base64:utf-8",
			compactLinesGzipBase64: await gzipBase64(
				JSON.stringify([[1, 1, "old", "new", "", "Modified"]]),
			),
		} as CommitFileDiffResponse;
		const first = loadCompactLines(diff);
		expect(loadCompactLines(diff)).toBe(first);

		releaseCompactLines(diff);

		expect(loadCompactLines(diff)).not.toBe(first);
	});
});

async function gzipBase64(value: string) {
	const input = new TextEncoder().encode(value);
	const stream = new Blob([input])
		.stream()
		.pipeThrough(new CompressionStream("gzip"));
	const compressed = new Uint8Array(await new Response(stream).arrayBuffer());
	return btoa(String.fromCharCode(...compressed));
}
