import type { CommitFileDiffResponse } from "@/generated/types";

export function hasVirtualTextPayload(diff: CommitFileDiffResponse) {
	return Boolean(diff.virtualText || diff.virtualTextGzipBase64);
}

export async function loadVirtualText(diff: CommitFileDiffResponse) {
	if (diff.virtualText) {
		return diff.virtualText;
	}

	if (diff.virtualTextEncoding !== "gzip-base64:utf-8") {
		throw new Error("Unsupported virtual diff payload encoding.");
	}

	const compressed = base64ToBytes(diff.virtualTextGzipBase64);
	const stream = new Blob([compressed])
		.stream()
		.pipeThrough(new DecompressionStream("gzip"));
	const buffer = await new Response(stream).arrayBuffer();
	return new TextDecoder().decode(buffer);
}

function base64ToBytes(value: string) {
	const binary = atob(value);
	const bytes = new Uint8Array(binary.length);
	for (let index = 0; index < binary.length; index++) {
		bytes[index] = binary.charCodeAt(index);
	}

	return bytes;
}
