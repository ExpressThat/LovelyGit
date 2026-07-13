import type {
	CommitFileDiffLine,
	CommitFileDiffResponse,
} from "@/generated/types";

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
type DeltaReferenceTuple = [
	number | null,
	number | null,
	number | string,
	CompactSyntaxSpanTuple[]?,
	CompactSyntaxSpanTuple[]?,
	CompactSyntaxSpanTuple[]?,
	CompactChangeSpanTuple[]?,
	CompactChangeSpanTuple[]?,
	CompactChangeSpanTuple[]?,
];

const decodedLineCache = new WeakMap<
	CommitFileDiffResponse,
	Promise<CommitFileDiffLine[]>
>();

export function hasCompactLinePayload(diff: CommitFileDiffResponse) {
	return Boolean(diff.compactLinesGzipBase64);
}

export function loadCompactLines(diff: CommitFileDiffResponse) {
	const cached = decodedLineCache.get(diff);
	if (cached) return cached;
	const loading = decodeCompactLines(diff);
	decodedLineCache.set(diff, loading);
	return loading;
}

async function decodeCompactLines(diff: CommitFileDiffResponse) {
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
) {
	if (diff.compactLineSchema === "tuple-v4-delta-refs:gzip-base64:utf-8") {
		const json = await decodeGzipBase64(diff.compactLinesGzipBase64);
		return decodeDeltaReferenceLines(
			JSON.parse(json) as DeltaReferenceTuple[],
			oldText,
			newText,
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

export function decodeDeltaReferenceLines(
	tuples: DeltaReferenceTuple[],
	oldText: string,
	newText: string,
) {
	const oldLines = textLines(oldText);
	const newLines = textLines(newText);
	let previousOld = 0;
	let previousNew = 0;
	return tuples.map((tuple): CommitFileDiffLine => {
		const oldLineNumber = applyDelta(tuple[0], previousOld);
		const newLineNumber = applyDelta(tuple[1], previousNew);
		if (oldLineNumber != null) previousOld = oldLineNumber;
		if (newLineNumber != null) previousNew = newLineNumber;
		return {
			oldLineNumber,
			newLineNumber,
			oldText: lineAt(oldLines, oldLineNumber),
			newText: lineAt(newLines, newLineNumber),
			text: "",
			changeType: changeType(tuple[2]),
			oldSyntaxSpans: (tuple[3] ?? []).map(toSyntaxSpan),
			newSyntaxSpans: (tuple[4] ?? []).map(toSyntaxSpan),
			syntaxSpans: (tuple[5] ?? []).map(toSyntaxSpan),
			oldChangeSpans: (tuple[6] ?? []).map(toChangeSpan),
			newChangeSpans: (tuple[7] ?? []).map(toChangeSpan),
			changeSpans: (tuple[8] ?? []).map(toChangeSpan),
		};
	});
}

function applyDelta(delta: number | null, previous: number) {
	return delta == null ? null : previous + delta;
}

function changeType(value: number | string) {
	if (typeof value === "string") return value;
	return (
		["Unchanged", "Modified", "Deleted", "Inserted", "Added", "Imaginary"][
			value
		] ?? ""
	);
}

export async function decodeGzipBase64(value: string) {
	return new TextDecoder().decode(await decodeGzipBase64Bytes(value));
}

export async function decodeGzipBase64Bytes(value: string) {
	const bytes = Uint8Array.from(atob(value), (character) =>
		character.charCodeAt(0),
	);
	const stream = new Blob([bytes])
		.stream()
		.pipeThrough(new DecompressionStream("gzip"));
	const buffer = await new Response(stream).arrayBuffer();
	return new Uint8Array(buffer);
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
		oldSyntaxSpans: (tuple[6] ?? []).map(toSyntaxSpan),
		newSyntaxSpans: (tuple[7] ?? []).map(toSyntaxSpan),
		syntaxSpans: (tuple[8] ?? []).map(toSyntaxSpan),
		oldChangeSpans: (tuple[9] ?? []).map(toChangeSpan),
		newChangeSpans: (tuple[10] ?? []).map(toChangeSpan),
		changeSpans: (tuple[11] ?? []).map(toChangeSpan),
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

function toChangeSpan([start, length, changeType]: CompactChangeSpanTuple) {
	return { start, length, changeType };
}
