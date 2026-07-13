const SOURCE_COUNT = 3;
const MAXIMUM_ROWS = 4 * 1024 * 1024;
const MAXIMUM_ENCODED_TEXT_BYTES = 256 * 1024 * 1024;

export function decodeConflictTextBundle(bytes: Uint8Array) {
	const cursor = { value: 0 };
	const rowCount = readVarUInt(bytes, cursor);
	if (rowCount > MAXIMUM_ROWS) throw invalidBundle("row count is too large");
	const rowsStart = cursor.value;
	const sourceLengths = [0, 0, 0];
	for (let row = 0; row < rowCount; row++) {
		for (let source = 0; source < SOURCE_COUNT; source++) {
			const length = readTextLength(bytes, cursor);
			if (length !== null) sourceLengths[source] += length;
			skip(bytes, cursor, length ?? 0);
		}
	}
	const resultLength = readTextLength(bytes, cursor) ?? 0;
	const resultStart = cursor.value;
	skip(bytes, cursor, resultLength);
	if (cursor.value !== bytes.length)
		throw invalidBundle("contains trailing data");

	const sources = sourceLengths.map((length) => new Uint8Array(length));
	const sourceOffsets = [0, 0, 0];
	cursor.value = rowsStart;
	for (let row = 0; row < rowCount; row++) {
		for (let source = 0; source < SOURCE_COUNT; source++) {
			const length = readTextLength(bytes, cursor);
			if (length !== null) {
				sources[source].set(
					bytes.subarray(cursor.value, cursor.value + length),
					sourceOffsets[source],
				);
				sourceOffsets[source] += length;
			}
			skip(bytes, cursor, length ?? 0);
		}
	}
	const decoder = new TextDecoder("utf-8", { fatal: true });
	return [
		...sources.map((source) => decoder.decode(source)),
		decoder.decode(bytes.subarray(resultStart, resultStart + resultLength)),
	];
}

function readTextLength(bytes: Uint8Array, cursor: { value: number }) {
	const encoded = readVarUInt(bytes, cursor);
	if (encoded === 0) return null;
	const length = encoded - 1;
	if (length > MAXIMUM_ENCODED_TEXT_BYTES)
		throw invalidBundle("text is too large");
	return length;
}

function readVarUInt(bytes: Uint8Array, cursor: { value: number }) {
	let value = 0;
	for (let index = 0; index < 5; index++) {
		if (cursor.value >= bytes.length) throw invalidBundle("is truncated");
		const next = bytes[cursor.value++];
		if (index === 4 && (next & 0xf0) !== 0)
			throw invalidBundle("contains an invalid integer");
		value += (next & 0x7f) * 2 ** (index * 7);
		if ((next & 0x80) === 0) return value;
	}
	throw invalidBundle("contains an invalid integer");
}

function skip(bytes: Uint8Array, cursor: { value: number }, length: number) {
	if (
		!Number.isSafeInteger(length) ||
		length < 0 ||
		cursor.value + length > bytes.length
	) {
		throw invalidBundle("is truncated");
	}
	cursor.value += length;
}

function invalidBundle(reason: string) {
	return new Error(`The compact conflict text bundle ${reason}.`);
}
