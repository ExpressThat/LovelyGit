import { describe, expect, it } from "vitest";
import { decodeConflictTextBundle } from "./conflictTextBinary";

describe("decodeConflictTextBundle", () => {
	it("reconstructs interleaved sources and unicode result text", () => {
		const bytes: number[] = [];
		writeVarUInt(bytes, 2);
		writeText(bytes, "base 1\n");
		writeText(bytes, "current 1\n");
		writeText(bytes, null);
		writeText(bytes, "base 2");
		writeText(bytes, "current 2");
		writeText(bytes, "incoming 2");
		writeText(bytes, "result 🙂");

		expect(decodeConflictTextBundle(new Uint8Array(bytes))).toEqual([
			"base 1\nbase 2",
			"current 1\ncurrent 2",
			"incoming 2",
			"result 🙂",
		]);
	});

	it.each([
		[new Uint8Array([1]), "truncated"],
		[new Uint8Array([0, 1, 0]), "trailing data"],
		[new Uint8Array([0x80, 0x80, 0x80, 0x80, 0x80]), "invalid integer"],
		[new Uint8Array([0xff, 0xff, 0xff, 0xff, 0x10]), "invalid integer"],
		[new Uint8Array([0xff, 0xff, 0xff, 0xff, 0x0f]), "row count is too large"],
		[new Uint8Array([0, 0xff, 0xff, 0xff, 0xff, 0x0f]), "text is too large"],
	])("rejects malformed payloads", (bytes, message) => {
		expect(() => decodeConflictTextBundle(bytes)).toThrow(message);
	});
});

function writeText(target: number[], text: string | null) {
	if (text === null) {
		writeVarUInt(target, 0);
		return;
	}
	const bytes = new TextEncoder().encode(text);
	writeVarUInt(target, bytes.length + 1);
	target.push(...bytes);
}

function writeVarUInt(target: number[], initial: number) {
	let value = initial;
	do {
		let next = value & 0x7f;
		value >>>= 7;
		if (value > 0) next |= 0x80;
		target.push(next);
	} while (value > 0);
}
