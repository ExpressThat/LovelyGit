import { describe, expect, it } from "vitest";
import { response } from "./ConflictResolutionViewTestFixtures";
import { loadConflictTextPayloads } from "./conflictTextPayload";

describe("conflictTextPayload", () => {
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
});

async function gzipBase64(text: string) {
	const stream = new Blob([text])
		.stream()
		.pipeThrough(new CompressionStream("gzip"));
	const bytes = new Uint8Array(await new Response(stream).arrayBuffer());
	return btoa(String.fromCharCode(...bytes));
}
