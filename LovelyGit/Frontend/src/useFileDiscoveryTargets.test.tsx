// @vitest-environment jsdom

import { act, renderHook } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { useFileDiscoveryTargets } from "./useFileDiscoveryTargets";

describe("useFileDiscoveryTargets", () => {
	it("opens and resets history and blame independently", () => {
		const { result } = renderHook(() => useFileDiscoveryTargets());

		act(() => result.current.openHistory("history.ts", "abc"));
		act(() => result.current.openBlame("blame.ts", null));
		expect(result.current.historyTarget).toEqual({
			path: "history.ts",
			startCommitHash: "abc",
		});
		expect(result.current.blameTarget).toEqual({
			path: "blame.ts",
			startCommitHash: null,
		});

		act(() => result.current.reset());
		expect(result.current.historyTarget).toBeNull();
		expect(result.current.blameTarget).toBeNull();
	});
});
