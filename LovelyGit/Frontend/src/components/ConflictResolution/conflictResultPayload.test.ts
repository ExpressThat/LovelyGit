import { describe, expect, it } from "vitest";
import { decodeGzipBase64 } from "@/components/CommitFileDiff/compactPayloadCompression";
import { prepareConflictResultPayload } from "./conflictResultPayload";

describe("prepareConflictResultPayload", () => {
	it("keeps small results inline", async () => {
		expect(await prepareConflictResultPayload("resolved\n")).toEqual({
			resultText: "resolved\n",
			resultTextGzipBase64: "",
		});
	});

	it("compresses a large result without changing its text", async () => {
		const expected = `${"shared line\n".repeat(100_000)}emoji 🚀\r\n`;
		const payload = await prepareConflictResultPayload(expected);

		expect(payload.resultText).toBeNull();
		expect(payload.resultTextGzipBase64.length).toBeLessThan(10_000);
		expect(await decodeGzipBase64(payload.resultTextGzipBase64)).toBe(expected);
	});
});
