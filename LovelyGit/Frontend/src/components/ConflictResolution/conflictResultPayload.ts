import { encodeGzipBase64 } from "@/components/CommitFileDiff/compactPayloadCompression";

const compressionThreshold = 64_000;

export async function prepareConflictResultPayload(resultText: string | null) {
	if (resultText === null || resultText.length < compressionThreshold) {
		return { resultText, resultTextGzipBase64: "" };
	}

	return {
		resultText: null,
		resultTextGzipBase64: await encodeGzipBase64(resultText),
	};
}
