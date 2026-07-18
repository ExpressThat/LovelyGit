import { decodeGzipBase64 } from "@/components/CommitFileDiff/compactPayloadCompression";
import type {
	BranchComparisonFile,
	BranchComparisonResponse,
} from "@/generated/types";

export const maximumBranchComparisonFiles = 20_000;

export async function expandBranchComparisonPayload(
	response: BranchComparisonResponse,
): Promise<BranchComparisonResponse> {
	if (!response.compactFilesGzipBase64) return response;

	const files: unknown = JSON.parse(
		await decodeGzipBase64(response.compactFilesGzipBase64),
	);
	if (!isBranchComparisonFileList(files)) {
		throw new Error("The branch comparison file list is invalid.");
	}

	return { ...response, compactFilesGzipBase64: null, files };
}

function isBranchComparisonFileList(
	value: unknown,
): value is BranchComparisonFile[] {
	return (
		Array.isArray(value) &&
		value.length <= maximumBranchComparisonFiles &&
		value.every(
			(file) =>
				typeof file === "object" &&
				file !== null &&
				typeof file.path === "string" &&
				typeof file.status === "string",
		)
	);
}
