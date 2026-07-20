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
	if (!Array.isArray(value) || value.length > maximumRemoteCount) return false;
	for (const candidate of value) {
		if (typeof candidate !== "object" || candidate === null) return false;
		const remote = candidate as Record<string, unknown>;
		if (
			typeof remote.name !== "string" ||
			typeof remote.url !== "string" ||
			(remote.pushUrl !== undefined &&
				remote.pushUrl !== null &&
				typeof remote.pushUrl !== "string")
		) {
			return false;
		}
		// Null properties are intentionally omitted by the compact C# serializer.
		if (remote.pushUrl === undefined) remote.pushUrl = null;
	}
	return true;
}
