// @vitest-environment jsdom

import { act, renderHook } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import type { CommitGraphRow } from "@/generated/types";
import { useCommitGraphDialogs } from "./useCommitGraphDialogs";

describe("useCommitGraphDialogs", () => {
	it("owns one bisect selection for the graph", () => {
		const { result } = renderHook(() => useCommitGraphDialogs());
		const commit = { rowIndex: 42 } as CommitGraphRow;

		act(() => result.current.setBisectCommit(commit));
		expect(result.current.bisectCommit).toBe(commit);

		act(() => result.current.setBisectCommit(null));
		expect(result.current.bisectCommit).toBeNull();
	});
});
