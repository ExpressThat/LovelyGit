import type { FileBlameResponse } from "@/generated/types";
import { decodeGzipBase64 } from "../CommitFileDiff/compactPayloadCompression";

export async function expandFileBlamePayload(response: FileBlameResponse) {
	if (!response.compactPayloadGzipBase64) return response;
	if (response.content || response.hunks.length) {
		throw new Error("File blame returned ambiguous compact content.");
	}

	return JSON.parse(
		await decodeGzipBase64(response.compactPayloadGzipBase64),
	) as FileBlameResponse;
}
