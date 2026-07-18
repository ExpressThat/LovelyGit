import { afterEach, describe, expect, it, vi } from "vitest";
import {
	decodeBase64Bytes,
	decodeGzipBase64,
	encodeGzipBase64,
} from "./compactPayloadCompression";

const originalDescriptor = Object.getOwnPropertyDescriptor(
	Uint8Array,
	"fromBase64",
);

afterEach(() => {
	vi.restoreAllMocks();
	if (originalDescriptor) {
		Object.defineProperty(Uint8Array, "fromBase64", originalDescriptor);
	} else {
		Reflect.deleteProperty(Uint8Array, "fromBase64");
	}
});

describe("decodeBase64Bytes", () => {
	it("uses the native typed-array decoder when WebView2 provides it", () => {
		const native = vi.fn(() => Uint8Array.of(3, 7, 11));
		Object.defineProperty(Uint8Array, "fromBase64", {
			configurable: true,
			value: native,
		});
		const legacy = vi.spyOn(globalThis, "atob");

		expect(decodeBase64Bytes("native payload")).toEqual(
			Uint8Array.of(3, 7, 11),
		);
		expect(native).toHaveBeenCalledWith("native payload");
		expect(legacy).not.toHaveBeenCalled();
	});

	it("decodes every byte without callback allocations on older runtimes", () => {
		Object.defineProperty(Uint8Array, "fromBase64", {
			configurable: true,
			value: undefined,
		});

		expect(decodeBase64Bytes("AH+A/w==")).toEqual(
			Uint8Array.of(0, 127, 128, 255),
		);
	});

	it("surfaces malformed input instead of returning partial bytes", () => {
		Object.defineProperty(Uint8Array, "fromBase64", {
			configurable: true,
			value: undefined,
		});

		expect(() => decodeBase64Bytes("%%% invalid %%%")).toThrow();
	});
});

describe("encodeGzipBase64", () => {
	it("round-trips a large UTF-8 specification", async () => {
		const source = `${"src/module\n".repeat(10_000)}café`;

		const encoded = await encodeGzipBase64(source);

		expect(encoded.length).toBeLessThan(source.length / 10);
		await expect(decodeGzipBase64(encoded)).resolves.toBe(source);
	});
});
