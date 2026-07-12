import type {
	ConflictFileVersion,
	ConflictResolutionResponse,
} from "@/generated/types";
import { decodeGzipBase64 } from "../CommitFileDiff/compactLinePayload";

const GZIP_TEXT_ENCODING = "gzip-base64:utf-8";

export async function loadConflictTextPayloads(
	conflict: ConflictResolutionResponse,
) {
	const [base, ours, theirs, result] = await Promise.all([
		loadVersion(conflict.base),
		loadVersion(conflict.ours),
		loadVersion(conflict.theirs),
		loadVersion(conflict.result),
	]);
	return { ...conflict, base, ours, theirs, result };
}

async function loadVersion(version: ConflictFileVersion) {
	if (!version.textGzipBase64) return version;
	if (version.textEncoding !== GZIP_TEXT_ENCODING) {
		throw new Error(
			`Unsupported conflict text encoding: ${version.textEncoding}`,
		);
	}
	return {
		...version,
		text: await decodeGzipBase64(version.textGzipBase64),
	};
}
