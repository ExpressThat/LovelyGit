import type {
	CommitFileDiffChangeSpan,
	CommitFileDiffLine,
	CommitFileDiffResponse,
	CommitFileDiffSyntaxSpan,
} from "@/generated/types";
import { getCompactDiffPayloadIdentity } from "@/lib/compactDiffPayloadIdentity";
import { decodeConflictTextBundle } from "../ConflictResolution/conflictTextBinary";
import {
	decodeGzipBase64,
	decodeGzipBase64Bytes,
} from "./compactPayloadCompression";
import {
	type DeltaReferenceTuple,
	decodeDeltaReferenceLineArrays,
	decodeDeltaReferenceLines,
} from "./deltaReferenceLines";

export { decodeDeltaReferenceLines } from "./deltaReferenceLines";

type CompactLineTuple = [
	number | null,
	number | null,
	string | null,
	string | null,
	string | null,
	string | null,
	CompactSyntaxSpanTuple[]?,
	CompactSyntaxSpanTuple[]?,
	CompactSyntaxSpanTuple[]?,
	CompactChangeSpanTuple[]?,
	CompactChangeSpanTuple[]?,
	CompactChangeSpanTuple[]?,
];

type CompactSyntaxSpanTuple = [number, number, string];
type CompactChangeSpanTuple = [number, number, string];
const decodedLineCache = new WeakMap<
	CommitFileDiffResponse,
	Promise<CommitFileDiffLine[]>
>();
const decodedDeltaTupleCache = new WeakMap<
	object,
	Promise<DeltaReferenceTuple[]>
>();
const decodedSourceCache = new WeakMap<
	object,
	Promise<{ oldLines: string[]; newLines: string[] }>
>();
const MAX_RETAINED_DECODED_LINES = 5_000;
const EMPTY_SYNTAX_SPANS: CommitFileDiffSyntaxSpan[] = [];
const EMPTY_CHANGE_SPANS: CommitFileDiffChangeSpan[] = [];
Object.freeze(EMPTY_SYNTAX_SPANS);
Object.freeze(EMPTY_CHANGE_SPANS);

export function hasCompactLinePayload(diff: CommitFileDiffResponse) {
	return Boolean(diff.compactLinesGzipBase64);
}

export function loadCompactLines(
	diff: CommitFileDiffResponse,
): Promise<CommitFileDiffLine[]> {
	const cached = decodedLineCache.get(diff);
	if (cached) return cached;
	const loading = decodeCompactLines(diff);
	decodedLineCache.set(diff, loading);
	return loading;
}

export function releaseCompactLines(diff: CommitFileDiffResponse) {
	if (!isOversizedDecodedPayload(diff)) return;
	decodedLineCache.delete(diff);
	releaseDeltaIntermediates(diff);
}

async function decodeCompactLines(
	diff: CommitFileDiffResponse,
): Promise<CommitFileDiffLine[]> {
	if (diff.compactLineSchema === "tuple-v4-delta-refs:gzip-base64:utf-8") {
		if (
			diff.compactSourceSchema !==
			"interleaved-lines-v3:gzip-base64:varint-utf-8"
		) {
			throw new Error(
				`Unsupported compact diff source schema: ${diff.compactSourceSchema}`,
			);
		}
		if (!diff.compactSourceBundleGzipBase64) {
			throw new Error("The compact diff source bundle is missing.");
		}
		try {
			const [sources, tuples] = await Promise.all([
				loadDeltaSources(diff),
				loadDeltaTuples(diff),
			]);
			return decodeDeltaReferenceLineArrays(
				tuples,
				sources.oldLines,
				sources.newLines,
				diff.viewMode === "Combined",
			);
		} finally {
			releaseDeltaSources(diff);
			if (isOversizedDecodedPayload(diff)) releaseDeltaTuples(diff);
		}
	}
	if (
		diff.compactLineSchema !== "tuple-v1:gzip-base64:utf-8" &&
		diff.compactLineSchema !== "tuple-v2:gzip-base64:utf-8"
	) {
		throw new Error(
			`Unsupported compact diff schema: ${diff.compactLineSchema}`,
		);
	}
	const json = await decodeGzipBase64(diff.compactLinesGzipBase64);
	const tuples = JSON.parse(json) as CompactLineTuple[];
	return tuples.map((tuple) => toDiffLine(tuple));
}

export async function loadReferencedCompactLines(
	diff: CommitFileDiffResponse,
	oldText: string,
	newText: string,
): Promise<CommitFileDiffLine[]> {
	if (diff.compactLineSchema === "tuple-v4-delta-refs:gzip-base64:utf-8") {
		const tuples = await loadDeltaTuples(diff);
		return decodeDeltaReferenceLines(
			tuples,
			oldText,
			newText,
			diff.viewMode === "Combined",
		);
	}
	if (diff.compactLineSchema !== "tuple-v3-refs:gzip-base64:utf-8") {
		return loadCompactLines(diff);
	}
	const json = await decodeGzipBase64(diff.compactLinesGzipBase64);
	const tuples = JSON.parse(json) as CompactLineTuple[];
	const sources = {
		oldLines: textLines(oldText),
		newLines: textLines(newText),
	};
	return tuples.map((tuple) => toDiffLine(tuple, sources));
}

function loadDeltaTuples(diff: CommitFileDiffResponse) {
	const identity = getCompactDiffPayloadIdentity(diff);
	let loading = decodedDeltaTupleCache.get(identity);
	if (!loading) {
		loading = decodeGzipBase64(diff.compactLinesGzipBase64).then(
			(json) => JSON.parse(json) as DeltaReferenceTuple[],
		);
		decodedDeltaTupleCache.set(identity, loading);
	}
	return loading;
}

function loadDeltaSources(diff: CommitFileDiffResponse) {
	const identity = getCompactDiffPayloadIdentity(diff);
	let loading = decodedSourceCache.get(identity);
	if (!loading) {
		loading = decodeGzipBase64Bytes(diff.compactSourceBundleGzipBase64).then(
			(sourceBytes) => {
				const [oldText, newText] = decodeConflictTextBundle(sourceBytes);
				return { oldLines: textLines(oldText), newLines: textLines(newText) };
			},
		);
		decodedSourceCache.set(identity, loading);
	}
	return loading;
}

function releaseDeltaIntermediates(diff: CommitFileDiffResponse) {
	releaseDeltaTuples(diff);
	releaseDeltaSources(diff);
}

function releaseDeltaTuples(diff: CommitFileDiffResponse) {
	decodedDeltaTupleCache.delete(getCompactDiffPayloadIdentity(diff));
}

function releaseDeltaSources(diff: CommitFileDiffResponse) {
	decodedSourceCache.delete(getCompactDiffPayloadIdentity(diff));
}

function isOversizedDecodedPayload(diff: CommitFileDiffResponse) {
	return (diff.compactLineCount ?? 0) > MAX_RETAINED_DECODED_LINES;
}

export function toDiffLine(
	tuple: CompactLineTuple,
	sources?: { oldLines: string[]; newLines: string[] },
): CommitFileDiffLine {
	return {
		oldLineNumber: tuple[0],
		newLineNumber: tuple[1],
		oldText: tuple[2] ?? lineAt(sources?.oldLines, tuple[0]),
		newText: tuple[3] ?? lineAt(sources?.newLines, tuple[1]),
		text: tuple[4] ?? "",
		changeType: tuple[5] ?? "",
		oldSyntaxSpans: mapSyntaxSpans(tuple[6]),
		newSyntaxSpans: mapSyntaxSpans(tuple[7]),
		syntaxSpans: mapSyntaxSpans(tuple[8]),
		oldChangeSpans: mapChangeSpans(tuple[9]),
		newChangeSpans: mapChangeSpans(tuple[10]),
		changeSpans: mapChangeSpans(tuple[11]),
	};
}

function textLines(text: string) {
	return text.replaceAll("\r\n", "\n").split("\n");
}

function lineAt(lines: string[] | undefined, oneBasedIndex: number | null) {
	return oneBasedIndex == null ? "" : (lines?.[oneBasedIndex - 1] ?? "");
}

function toSyntaxSpan([start, length, scope]: CompactSyntaxSpanTuple) {
	return { start, length, scope };
}

function mapSyntaxSpans(spans: CompactSyntaxSpanTuple[] | undefined) {
	return spans?.length ? spans.map(toSyntaxSpan) : EMPTY_SYNTAX_SPANS;
}

function toChangeSpan([start, length, changeType]: CompactChangeSpanTuple) {
	return { start, length, changeType };
}

function mapChangeSpans(spans: CompactChangeSpanTuple[] | undefined) {
	return spans?.length ? spans.map(toChangeSpan) : EMPTY_CHANGE_SPANS;
}
