import type {
	ConflictFileVersion,
	ConflictResolutionResponse,
} from "@/generated/types";
import {
	decodeGzipBase64,
	decodeGzipBase64Bytes,
} from "../CommitFileDiff/compactLinePayload";
import { decodeConflictTextBundle } from "./conflictTextBinary";

const GZIP_TEXT_ENCODING = "gzip-base64:utf-8";
const BINARY_BUNDLE_SCHEMA = "interleaved-lines-v3:gzip-base64:varint-utf-8";
const LEGACY_BUNDLE_SCHEMA = "interleaved-lines-v2:gzip-base64:utf-8";

export async function loadConflictTextPayloads(
	conflict: ConflictResolutionResponse,
	sibling?: ConflictResolutionResponse | null,
) {
	if (conflict.compactTextBundleGzipBase64) {
		const reusable = reusableBundleTexts(conflict, sibling);
		if (reusable) return withTexts(conflict, reusable);
		if (conflict.compactTextSchema === BINARY_BUNDLE_SCHEMA) {
			const bytes = await decodeGzipBase64Bytes(
				conflict.compactTextBundleGzipBase64,
			);
			return withTexts(conflict, decodeConflictTextBundle(bytes));
		}
		if (conflict.compactTextSchema !== LEGACY_BUNDLE_SCHEMA) {
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
		return withTexts(conflict, [texts[0], texts[1], texts[2], result ?? ""]);
	}
	const [base, ours, theirs, result] = await Promise.all([
		loadVersion(conflict.base),
		loadVersion(conflict.ours),
		loadVersion(conflict.theirs),
		loadVersion(conflict.result),
	]);
	return { ...conflict, base, ours, theirs, result };
}

function reusableBundleTexts(
	conflict: ConflictResolutionResponse,
	sibling?: ConflictResolutionResponse | null,
) {
	if (
		!sibling ||
		conflict.worktreeFingerprint !== sibling.worktreeFingerprint ||
		conflict.compactTextSchema !== sibling.compactTextSchema ||
		conflict.compactTextBundleGzipBase64 !== sibling.compactTextBundleGzipBase64
	) {
		return null;
	}
	const texts = [
		sibling.base.text,
		sibling.ours.text,
		sibling.theirs.text,
		sibling.result.text,
	];
	return texts.every((text) => typeof text === "string")
		? (texts as string[])
		: null;
}

function withTexts(conflict: ConflictResolutionResponse, texts: string[]) {
	return {
		...conflict,
		base: { ...conflict.base, text: texts[0] },
		ours: { ...conflict.ours, text: texts[1] },
		theirs: { ...conflict.theirs, text: texts[2] },
		result: { ...conflict.result, text: texts[3] },
	};
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
