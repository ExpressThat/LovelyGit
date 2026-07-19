import { decodeGzipBase64 } from "@/components/CommitFileDiff/compactPayloadCompression";
import type {
	KnownGitRepositoriesResponse,
	KnownGitRepository,
} from "@/generated/types";

export async function expandKnownRepositories(
	response: KnownGitRepositoriesResponse,
): Promise<KnownGitRepository[]> {
	if (!response.compactRepositoriesGzipBase64) {
		return response.repositories;
	}

	return JSON.parse(
		await decodeGzipBase64(response.compactRepositoriesGzipBase64),
	) as KnownGitRepository[];
}
