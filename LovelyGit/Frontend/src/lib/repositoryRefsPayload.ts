import { decodeGzipBase64 } from "@/components/CommitFileDiff/compactPayloadCompression";
import type {
	RepositoryRefItem,
	RepositoryRefsResponse,
} from "@/generated/types";

export async function expandRepositoryRefsPayload(
	response: RepositoryRefsResponse,
): Promise<RepositoryRefsResponse> {
	if (!response.compactRefsGzipBase64) return response;

	const refs = JSON.parse(
		await decodeGzipBase64(response.compactRefsGzipBase64),
	) as RepositoryRefItem[];
	return { ...response, compactRefsGzipBase64: null, refs };
}
