import { decodeGzipBase64 } from "@/components/CommitFileDiff/compactPayloadCompression";
import type { GetRemotesResponse, GitRemote } from "@/generated/types";

export const maximumRemoteCount = 20_000;

export async function expandRemotePayload(
	response: GetRemotesResponse,
): Promise<GitRemote[]> {
	if (!response.compactRemotesGzipBase64) return response.remotes;

	const remotes: unknown = JSON.parse(
		await decodeGzipBase64(response.compactRemotesGzipBase64),
	);
	if (!isRemoteList(remotes)) {
		throw new Error("The remote list is invalid.");
	}

	return remotes;
}

function isRemoteList(value: unknown): value is GitRemote[] {
	return (
		Array.isArray(value) &&
		value.length <= maximumRemoteCount &&
		value.every(
			(remote) =>
				typeof remote === "object" &&
				remote !== null &&
				typeof remote.name === "string" &&
				typeof remote.url === "string" &&
				(remote.pushUrl === null || typeof remote.pushUrl === "string"),
		)
	);
}
