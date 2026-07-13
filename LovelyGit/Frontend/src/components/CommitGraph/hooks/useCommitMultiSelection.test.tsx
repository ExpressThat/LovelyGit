// @vitest-environment jsdom

import { act, renderHook } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { CommitGraphRow } from "@/generated/types";
import {
	orderSelectedCommits,
	useCommitMultiSelection,
} from "./useCommitMultiSelection";

describe("useCommitMultiSelection", () => {
	it("toggles commits and replaces the set with a shift range", () => {
		const rows = [row("a"), row("b"), row("c"), row("d")];
		const { result } = renderHook(() => useCommitMultiSelection("repo", rows));
		act(() => result.current.select(rows[1], 1, ctrl(), vi.fn()));
		act(() => result.current.select(rows[3], 3, shift(), vi.fn()));

		expect([...result.current.hashes]).toEqual([
			rows[1].commit.hash,
			rows[2].commit.hash,
			rows[3].commit.hash,
		]);

		act(() => result.current.select(rows[2], 2, ctrl(), vi.fn()));
		expect(result.current.hashes.has(rows[2].commit.hash)).toBe(false);
	});

	it("opens a normal click and clears operation selection", () => {
		const rows = [row("a"), row("b")];
		const onOpen = vi.fn();
		const { result } = renderHook(() => useCommitMultiSelection("repo", rows));
		act(() => result.current.select(rows[0], 0, ctrl(), onOpen));
		act(() => result.current.select(rows[1], 1, plain(), onOpen));

		expect(result.current.count).toBe(0);
		expect(onOpen).toHaveBeenCalledWith(rows[1]);
	});

	it("orders cherry-picks oldest first and reverts newest first", () => {
		const rows = [row("a"), row("b"), row("c")];
		const hashes = new Set(rows.map((item) => item.commit.hash));

		expect(orderSelectedCommits(rows, hashes, "cherry-pick")).toEqual([
			rows[2],
			rows[1],
			rows[0],
		]);
		expect(orderSelectedCommits(rows, hashes, "revert")).toEqual(rows);
	});
});

function row(digit: string) {
	return {
		commit: { hash: digit.repeat(40), message: digit },
	} as CommitGraphRow;
}

function plain() {
	return { ctrlKey: false, metaKey: false, shiftKey: false };
}

function ctrl() {
	return { ...plain(), ctrlKey: true };
}

function shift() {
	return { ...plain(), shiftKey: true };
}
