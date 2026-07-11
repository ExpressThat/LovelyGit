// @vitest-environment jsdom

import { act, renderHook, waitFor } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { useRefsAccordionLayout } from "./useRefsAccordionLayout";

describe("useRefsAccordionLayout", () => {
	it("opens every pane independently with equal default weight", async () => {
		const { result } = renderHook(() =>
			useRefsAccordionLayout(["Worktrees", "Branches", "Tags"]),
		);
		await waitFor(() => expect(result.current.weights.Branches).toBe(1));

		expect(result.current.openIds).toEqual(["Worktrees", "Branches", "Tags"]);
		act(() => result.current.toggle("Branches"));
		expect(result.current.openIds).toEqual(["Worktrees", "Tags"]);
		act(() => result.current.toggle("Branches"));
		expect(result.current.openIds).toEqual(["Worktrees", "Branches", "Tags"]);
	});

	it("resizes adjacent panes while preserving their combined weight", async () => {
		const { result } = renderHook(() =>
			useRefsAccordionLayout(["Branches", "Tags"]),
		);
		await waitFor(() => expect(result.current.weights.Tags).toBe(1));

		act(() => result.current.resize("Branches", "Tags", 0.5));
		expect(result.current.weights).toMatchObject({ Branches: 1.5, Tags: 0.5 });
		act(() => result.current.resize("Branches", "Tags", 5));
		expect(result.current.weights.Branches).toBe(1.8);
		expect(result.current.weights.Tags).toBeCloseTo(0.2);
	});
});
