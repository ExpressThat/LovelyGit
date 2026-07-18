import { decodeGzipBase64 } from "@/components/CommitFileDiff/compactPayloadCompression";

export async function expandCommitPatchPayload<
	T extends { compactPatchGzipBase64: string; patch: string },
>(response: T): Promise<T> {
	if (!response.compactPatchGzipBase64) return response;
	if (response.patch)
		throw new Error("Commit patch returned ambiguous content.");

	return {
		...response,
		compactPatchGzipBase64: "",
		patch: await decodeGzipBase64(response.compactPatchGzipBase64),
	};
}
