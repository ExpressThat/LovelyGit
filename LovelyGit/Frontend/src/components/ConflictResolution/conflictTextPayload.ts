import type {
	ConflictFileVersion,
	ConflictResolutionResponse,
} from "@/generated/types";
import { decodeGzipBase64 } from "../CommitFileDiff/compactLinePayload";

const GZIP_TEXT_ENCODING = "gzip-base64:utf-8";
const BUNDLE_SCHEMA = "interleaved-lines-v2:gzip-base64:utf-8";

export async function loadConflictTextPayloads(
	conflict: ConflictResolutionResponse,
) {
	if (conflict.compactTextBundleGzipBase64) {
		if (conflict.compactTextSchema !== BUNDLE_SCHEMA) {
			throw new Error(
				`Unsupported conflict text bundle: ${conflict.compactTextSchema}`,
			);
		}
		const json = await decodeGzipBase64(conflict.compactTextBundleGzipBase64);
		const [rows, result] = JSON.parse(json) as [
			Array<[string | null, string | null, string | null]>,
			string | null,
		];
		const texts = [0, 1, 2].map((index) =>
			rows.map((row) => row[index] ?? "").join(""),
		);
		return {
			...conflict,
			base: { ...conflict.base, text: texts[0] },
			ours: { ...conflict.ours, text: texts[1] },
			theirs: { ...conflict.theirs, text: texts[2] },
			result: { ...conflict.result, text: result ?? "" },
		};
	}
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
