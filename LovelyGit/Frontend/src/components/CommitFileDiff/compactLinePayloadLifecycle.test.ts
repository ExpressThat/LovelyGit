import { describe, expect, it } from "vitest";
import type { CommitFileDiffResponse } from "@/generated/types";
import { shareCompactDiffPayloadIdentity } from "@/lib/compactDiffPayloadIdentity";
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

	it("releases large source arrays even when the rendered diff is small", async () => {
		const diff = await deltaDiff();
		const alternate = {
			...diff,
			viewMode: "SideBySide",
		} as CommitFileDiffResponse;
		shareCompactDiffPayloadIdentity(diff, alternate);
		const NativeDecompressionStream = globalThis.DecompressionStream;
		let decompressions = 0;
		globalThis.DecompressionStream = class extends NativeDecompressionStream {
			constructor(format: CompressionFormat) {
				decompressions += 1;
				super(format);
			}
		};

		try {
			await loadCompactLines(diff);
			await loadCompactLines(alternate);
			expect(decompressions).toBe(3);
		} finally {
			globalThis.DecompressionStream = NativeDecompressionStream;
		}
	});
});

async function deltaDiff() {
	return {
		compactLineCount: 1,
		compactLineSchema: "tuple-v4-delta-refs:gzip-base64:utf-8",
		compactLinesGzipBase64: await gzipBase64(JSON.stringify([[1, 1, 1]])),
		compactSourceSchema: "interleaved-lines-v3:gzip-base64:varint-utf-8",
		compactSourceBundleGzipBase64: await gzipBytesBase64(
			encodeSourceBundle("before", "after"),
		),
		viewMode: "Combined",
	} as CommitFileDiffResponse;
}

async function gzipBase64(value: string) {
	const input = new TextEncoder().encode(value);
	const stream = new Blob([input])
		.stream()
		.pipeThrough(new CompressionStream("gzip"));
	const compressed = new Uint8Array(await new Response(stream).arrayBuffer());
	return btoa(String.fromCharCode(...compressed));
}

async function gzipBytesBase64(bytes: Uint8Array) {
	const stream = new Blob([bytes.buffer as ArrayBuffer])
		.stream()
		.pipeThrough(new CompressionStream("gzip"));
	const compressed = new Uint8Array(await new Response(stream).arrayBuffer());
	return btoa(String.fromCharCode(...compressed));
}

function encodeSourceBundle(oldText: string, newText: string) {
	const output: number[] = [1];
	for (const value of [oldText, newText, undefined, undefined]) {
		if (value === undefined) {
			output.push(0);
			continue;
		}
		const bytes = new TextEncoder().encode(value);
		output.push(bytes.length + 1, ...bytes);
	}
	return Uint8Array.from(output);
}
