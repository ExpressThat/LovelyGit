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
