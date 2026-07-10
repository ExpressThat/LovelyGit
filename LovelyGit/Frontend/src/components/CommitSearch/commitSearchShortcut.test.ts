import { describe, expect, it } from "vitest";
import { isCommitSearchShortcut } from "./commitSearchShortcut";

describe("isCommitSearchShortcut", () => {
	it("accepts Ctrl+F and Command+F", () => {
		expect(shortcut({ ctrlKey: true, key: "f" })).toBe(true);
		expect(shortcut({ key: "F", metaKey: true })).toBe(true);
	});

	it("rejects unmodified and alternate shortcuts", () => {
		expect(shortcut({ key: "f" })).toBe(false);
		expect(shortcut({ altKey: true, ctrlKey: true, key: "f" })).toBe(false);
		expect(shortcut({ ctrlKey: true, key: "k" })).toBe(false);
	});
});

function shortcut(
	values: Partial<
		Pick<KeyboardEvent, "altKey" | "ctrlKey" | "key" | "metaKey">
	>,
) {
	return isCommitSearchShortcut({
		altKey: false,
		ctrlKey: false,
		key: "",
		metaKey: false,
		...values,
	} as KeyboardEvent);
}
