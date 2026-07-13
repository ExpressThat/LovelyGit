import { describe, expect, it, vi } from "vitest";
import type { CommitFileDiffResponse } from "@/generated/types";
import {
	cacheCommitFileDiffViews,
	clearCommitFileDiffCache,
	getCachedCommitFileDiff,
} from "@/lib/commitFileDiffCache";
import { loadCompactLines } from "./compactLinePayload";

describe("compact line view projection", () => {
	it("decompresses shared source and tuples only once across view modes", async () => {
		const combined = {
			compactLineSchema: "tuple-v4-delta-refs:gzip-base64:utf-8",
			compactLinesGzipBase64: await gzipBase64(JSON.stringify([[1, 1, 1]])),
			compactSourceSchema: "interleaved-lines-v3:gzip-base64:varint-utf-8",
			compactSourceBundleGzipBase64: await gzipBytesBase64(
				encodeSourceBundle("before", "after"),
			),
			viewMode: "Combined",
		} as CommitFileDiffResponse;
		clearCommitFileDiffCache();
		cacheCommitFileDiffViews("combined", "side-by-side", combined);
		const sideBySide = getCachedCommitFileDiff("side-by-side");
		expect(sideBySide).toBeDefined();
		const NativeDecompressionStream = globalThis.DecompressionStream;
		let decompressions = 0;
		vi.stubGlobal(
			"DecompressionStream",
			class extends NativeDecompressionStream {
				constructor(format: CompressionFormat) {
					decompressions += 1;
					super(format);
				}
			},
		);

		try {
			const [combinedLines, sideBySideLines] = await Promise.all([
				loadCompactLines(combined),
				loadCompactLines(sideBySide as CommitFileDiffResponse),
			]);
			expect(decompressions).toBe(2);
			expect(combinedLines.map((line) => line.changeType)).toEqual([
				"Deleted",
				"Inserted",
			]);
			expect(sideBySideLines.map((line) => line.changeType)).toEqual([
				"Modified",
			]);
		} finally {
			vi.unstubAllGlobals();
		}
	});
});

async function gzipBase64(value: string) {
	return gzipBytesBase64(new TextEncoder().encode(value));
}

async function gzipBytesBase64(bytes: Uint8Array) {
	const buffer = bytes.buffer.slice(
		bytes.byteOffset,
		bytes.byteOffset + bytes.byteLength,
	) as ArrayBuffer;
	const stream = new Blob([buffer])
		.stream()
		.pipeThrough(new CompressionStream("gzip"));
	const compressed = new Uint8Array(await new Response(stream).arrayBuffer());
	return btoa(String.fromCharCode(...compressed));
}

function encodeSourceBundle(oldText: string, newText: string) {
	const output: number[] = [];
	writeVarUInt(output, 1);
	for (const value of [oldText, newText, undefined, undefined]) {
		writeText(output, value);
	}
	return Uint8Array.from(output);
}

function writeText(output: number[], value: string | undefined) {
	if (value === undefined) {
		writeVarUInt(output, 0);
		return;
	}
	const bytes = new TextEncoder().encode(value);
	writeVarUInt(output, bytes.length + 1);
	output.push(...bytes);
}

function writeVarUInt(output: number[], initialValue: number) {
	let value = initialValue;
	do {
		let next = value & 0x7f;
		value >>>= 7;
		if (value !== 0) next |= 0x80;
		output.push(next);
	} while (value !== 0);
}
