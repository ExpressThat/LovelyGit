import { describe, expect, it } from "vitest";
import { response } from "./ConflictResolutionViewTestFixtures";
import { loadConflictTextPayloads } from "./conflictTextPayload";

describe("conflictTextPayload", () => {
	it("restores the shared conflict text bundle", async () => {
		const conflict = response();
		const expected = ["base", "current", "incoming", "result"];
		conflict.compactTextSchema =
			"interleaved-lines-v3:gzip-base64:varint-utf-8";
		conflict.compactTextBundleGzipBase64 = await gzipBase64Bytes(
			binaryBundle(expected),
		);
		for (const version of [
			conflict.base,
			conflict.ours,
			conflict.theirs,
			conflict.result,
		]) {
			version.text = "";
		}

		const loaded = await loadConflictTextPayloads(conflict);

		expect([
			loaded.base.text,
			loaded.ours.text,
			loaded.theirs.text,
			loaded.result.text,
		]).toEqual(expected);
	});

	it("restores the legacy JSON bundle", async () => {
		const conflict = response();
		conflict.compactTextSchema = "interleaved-lines-v2:gzip-base64:utf-8";
		conflict.compactTextBundleGzipBase64 = await gzipBase64(
			JSON.stringify([[["base", "current", "incoming"]], "result"]),
		);

		const loaded = await loadConflictTextPayloads(conflict);

		expect([
			loaded.base.text,
			loaded.ours.text,
			loaded.theirs.text,
			loaded.result.text,
		]).toEqual(["base", "current", "incoming", "result"]);
	});

	it("restores every compressed conflict version", async () => {
		const conflict = response();
		const expected = [
			"base text",
			"current text",
			"incoming text",
			"result text",
		];
		const versions = [
			conflict.base,
			conflict.ours,
			conflict.theirs,
			conflict.result,
		];
		for (const [index, version] of versions.entries()) {
			version.text = "";
			version.textEncoding = "gzip-base64:utf-8";
			version.textGzipBase64 = await gzipBase64(expected[index]);
		}

		const loaded = await loadConflictTextPayloads(conflict);

		expect([
			loaded.base.text,
			loaded.ours.text,
			loaded.theirs.text,
			loaded.result.text,
		]).toEqual(expected);
	});

	it("rejects unknown encodings instead of rendering corrupt text", async () => {
		const conflict = response();
		conflict.base.text = "";
		conflict.base.textGzipBase64 = "payload";
		conflict.base.textEncoding = "unknown";

		await expect(loadConflictTextPayloads(conflict)).rejects.toThrow(
			"Unsupported conflict text encoding: unknown",
		);
	});

	it("rejects an unknown shared bundle schema", async () => {
		const conflict = response();
		conflict.compactTextSchema = "unknown";
		conflict.compactTextBundleGzipBase64 = "payload";

		await expect(loadConflictTextPayloads(conflict)).rejects.toThrow(
			"Unsupported conflict text bundle: unknown",
		);
	});
});

async function gzipBase64(text: string) {
	const stream = new Blob([text])
		.stream()
		.pipeThrough(new CompressionStream("gzip"));
	const bytes = new Uint8Array(await new Response(stream).arrayBuffer());
	return btoa(String.fromCharCode(...bytes));
}

async function gzipBase64Bytes(bytes: Uint8Array) {
	const stream = new Blob([bytes.buffer as ArrayBuffer])
		.stream()
		.pipeThrough(new CompressionStream("gzip"));
	const compressed = new Uint8Array(await new Response(stream).arrayBuffer());
	return btoa(String.fromCharCode(...compressed));
}

function binaryBundle(texts: string[]) {
	const bytes = [1];
	for (const text of texts.slice(0, 3)) writeText(bytes, text);
	writeText(bytes, texts[3]);
	return new Uint8Array(bytes);
}

function writeText(target: number[], text: string) {
	const bytes = new TextEncoder().encode(text);
	let value = bytes.length + 1;
	do {
		let next = value & 0x7f;
		value >>>= 7;
		if (value > 0) next |= 0x80;
		target.push(next);
	} while (value > 0);
	target.push(...bytes);
}
