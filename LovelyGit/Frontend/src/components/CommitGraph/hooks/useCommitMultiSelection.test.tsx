// @vitest-environment jsdom

import { act, renderHook } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { CommitGraphRow } from "@/generated/types";
import {
	comparisonPair,
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
		expect(result.current.ordered("cherry-pick")).toEqual([rows[3], rows[1]]);
	});

	it("opens a normal click and clears operation selection", () => {
		const rows = [row("a"), row("b")];
		const onOpen = vi.fn();
		const { result } = renderHook(() => useCommitMultiSelection("repo", rows));
		act(() => result.current.select(rows[0], 0, ctrl(), onOpen));
		act(() => result.current.select(rows[1], 1, plain(), onOpen));

		expect(result.current.count).toBe(0);
		expect(result.current.ordered("revert")).toEqual([]);
		expect(onOpen).toHaveBeenCalledWith(rows[1]);
	});

	it("uses a normal click as the anchor for a later shift selection", () => {
		const rows = [row("a"), row("b"), row("c")];
		const { result } = renderHook(() => useCommitMultiSelection("repo", rows));
		act(() => result.current.select(rows[0], 0, plain(), vi.fn()));
		act(() => result.current.select(rows[2], 2, shift(), vi.fn()));

		expect(result.current.count).toBe(3);
	});

	it("orders cherry-picks oldest first and reverts newest first", () => {
		const rows = [row("a"), row("b"), row("c")];
		const selected = rows.map((item, index) => ({ index, row: item }));

		expect(orderSelectedCommits(selected, "cherry-pick")).toEqual([
			rows[2],
			rows[1],
			rows[0],
		]);
		expect(orderSelectedCommits(selected, "revert")).toEqual(rows);
	});

	it("compares exactly two commits from older base to newer target", () => {
		const rows = [row("a"), row("b"), row("c")];

		expect(
			comparisonPair([
				{ index: 2, row: rows[2] },
				{ index: 0, row: rows[0] },
			]),
		).toEqual({ base: rows[2], target: rows[0] });
		expect(comparisonPair([{ index: 0, row: rows[0] }])).toBeNull();
		expect(
			comparisonPair(rows.map((item, index) => ({ index, row: item }))),
		).toBeNull();
	});

	it("orders a bounded selection without scanning a sparse large graph", () => {
		const rows = new Array<CommitGraphRow | null>(500_000).fill(null);
		const selected = Array.from({ length: 100 }, (_, index) => {
			const selectedRow = row(String(index % 10));
			const graphIndex = index * 4_000;
			rows[graphIndex] = selectedRow;
			return { index: graphIndex, row: selectedRow };
		});
		const startedAt = performance.now();
		for (let iteration = 0; iteration < 100; iteration++) {
			orderSelectedCommits(selected, "cherry-pick");
		}

		expect(performance.now() - startedAt).toBeLessThan(20);
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
