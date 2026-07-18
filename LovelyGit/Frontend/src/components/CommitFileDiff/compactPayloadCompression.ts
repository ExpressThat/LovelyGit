export async function decodeGzipBase64(value: string) {
	return new TextDecoder().decode(await decodeGzipBase64Bytes(value));
}

export async function encodeGzipBase64(value: string) {
	const stream = new Blob([value])
		.stream()
		.pipeThrough(new CompressionStream("gzip"));
	const bytes = new Uint8Array(await new Response(stream).arrayBuffer());
	const nativeEncoder = (
		bytes as Uint8Array<ArrayBuffer> & { toBase64?: () => string }
	).toBase64;
	if (nativeEncoder) return nativeEncoder.call(bytes);

	let binary = "";
	const chunkSize = 32_768;
	for (let offset = 0; offset < bytes.length; offset += chunkSize) {
		binary += String.fromCharCode(
			...bytes.subarray(offset, offset + chunkSize),
		);
	}
	return btoa(binary);
}

export async function decodeGzipBase64Bytes(value: string) {
	const bytes = decodeBase64Bytes(value);
	const stream = new Blob([bytes])
		.stream()
		.pipeThrough(new DecompressionStream("gzip"));
	const buffer = await new Response(stream).arrayBuffer();
	return new Uint8Array(buffer);
}

export function decodeBase64Bytes(value: string): Uint8Array<ArrayBuffer> {
	const nativeDecoder = (
		Uint8Array as typeof Uint8Array & {
			fromBase64?: (input: string) => Uint8Array<ArrayBuffer>;
		}
	).fromBase64;
	if (nativeDecoder) return nativeDecoder.call(Uint8Array, value);

	const decoded = atob(value);
	const bytes = new Uint8Array(decoded.length);
	for (let index = 0; index < decoded.length; index++) {
		bytes[index] = decoded.charCodeAt(index);
	}
	return bytes;
}
