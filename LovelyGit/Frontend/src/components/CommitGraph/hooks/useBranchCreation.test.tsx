// @vitest-environment jsdom

import { act, renderHook } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import type { GitReflogEntry } from "@/generated/types";
import { useBranchCreation } from "./useBranchCreation";

describe("useBranchCreation", () => {
	it("creates a recovery branch source at the exact reflog commit", () => {
		const { result } = renderHook(() => useBranchCreation());
		const entry = reflogEntry();

		act(() => result.current.createFromReflog(entry));

		expect(result.current.source).toEqual({
			description: entry.message,
			label: entry.selector,
			startPoint: entry.newHash,
		});
	});

	it("describes a reflog entry without a message as a recovery point", () => {
		const { result } = renderHook(() => useBranchCreation());
		const entry = { ...reflogEntry(), message: "" };

		act(() => result.current.createFromReflog(entry));

		expect(result.current.source?.description).toBe(
			`Recovery point ${entry.selector}`,
		);
	});
});

function reflogEntry(): GitReflogEntry {
	return {
		actorEmail: "ross@example.invalid",
		actorName: "Ross",
		message: "reset: moving to HEAD~1",
		newHash: "abcdef1234567890abcdef1234567890abcdef12",
		oldHash: "1234567890abcdef1234567890abcdef12345678",
		selector: "main@{2}",
		timestampUnixSeconds: 1_700_000_000,
		timezone: "+0000",
	};
}
