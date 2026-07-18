import { describe, expect, it } from "vitest";
import { encodeGzipBase64 } from "@/components/CommitFileDiff/compactPayloadCompression";
import { expandCommitPatchPayload } from "./commitPatchPayload";

describe("expandCommitPatchPayload", () => {
	it("leaves an ordinary patch unchanged", async () => {
		const response = { compactPatchGzipBase64: "", patch: "patch" };

		expect(await expandCommitPatchPayload(response)).toBe(response);
	});

	it("restores a compressed patch", async () => {
		const response = {
			compactPatchGzipBase64: await encodeGzipBase64("large patch"),
			patch: "",
		};

		expect(await expandCommitPatchPayload(response)).toEqual({
			compactPatchGzipBase64: "",
			patch: "large patch",
		});
	});

	it("rejects ambiguous patch content", async () => {
		const response = { compactPatchGzipBase64: "compressed", patch: "patch" };

		await expect(expandCommitPatchPayload(response)).rejects.toThrow(
			"ambiguous",
		);
	});
});
