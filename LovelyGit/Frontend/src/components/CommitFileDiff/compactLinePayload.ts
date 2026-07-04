import type {
	CommitFileDiffLine,
	CommitFileDiffResponse,
} from "@/generated/types";

type CompactLineTuple = [
	number | null,
	number | null,
	string,
	string,
	string,
	string,
];

export function hasCompactLinePayload(diff: CommitFileDiffResponse) {
	return Boolean(diff.compactLinesGzipBase64);
}

export async function loadCompactLines(diff: CommitFileDiffResponse) {
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

function toDiffLine(tuple: CompactLineTuple): CommitFileDiffLine {
	return {
		oldLineNumber: tuple[0],
		newLineNumber: tuple[1],
		oldText: tuple[2],
		newText: tuple[3],
		text: tuple[4],
		changeType: tuple[5],
		oldSyntaxSpans: [],
		newSyntaxSpans: [],
		syntaxSpans: [],
		oldChangeSpans: [],
		newChangeSpans: [],
		changeSpans: [],
	};
}
