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

export function hasCompactLinePayload(diff: CommitFileDiffResponse) {
	return Boolean(diff.compactLinesGzipBase64);
}

export async function loadCompactLines(diff: CommitFileDiffResponse) {
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
	return tuples.map(toDiffLine);
}

async function decodeGzipBase64(value: string) {
	const bytes = Uint8Array.from(atob(value), (character) =>
		character.charCodeAt(0),
	);
	const stream = new Blob([bytes])
		.stream()
		.pipeThrough(new DecompressionStream("gzip"));
	const buffer = await new Response(stream).arrayBuffer();
	return new TextDecoder().decode(buffer);
}

export function toDiffLine(tuple: CompactLineTuple): CommitFileDiffLine {
	return {
		oldLineNumber: tuple[0],
		newLineNumber: tuple[1],
		oldText: tuple[2] ?? "",
		newText: tuple[3] ?? "",
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

function toSyntaxSpan([start, length, scope]: CompactSyntaxSpanTuple) {
	return { start, length, scope };
}

function toChangeSpan([start, length, changeType]: CompactChangeSpanTuple) {
	return { start, length, changeType };
}
