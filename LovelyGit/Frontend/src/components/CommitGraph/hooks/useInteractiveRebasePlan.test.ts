import { describe, expect, it } from "vitest";
import type { InteractiveRebasePlanItem } from "@/generated/types";
import { movePlanItem, validatePlan } from "./useInteractiveRebasePlan";

describe("interactive rebase plan", () => {
	it("reorders without mutating the previous plan", () => {
		const original = [item("a"), item("b"), item("c")];
		const moved = movePlanItem(original, 2, -1);
		expect(moved.map((entry) => entry.hash)).toEqual(["a", "c", "b"]);
		expect(original.map((entry) => entry.hash)).toEqual(["a", "b", "c"]);
	});

	it("keeps the same plan for an out-of-range move", () => {
		const original = [item("a")];
		expect(movePlanItem(original, 0, -1)).toBe(original);
	});

	it("requires a retained commit before squash or fixup", () => {
		expect(validatePlan([item("a", "Squash")])).toMatch(/retained/i);
		expect(validatePlan([item("a", "Drop"), item("b", "Fixup")])).toMatch(
			/retained/i,
		);
	});

	it("requires reword messages and one retained commit", () => {
		expect(validatePlan([item("a", "Reword", "  ")])).toMatch(/message/i);
		expect(validatePlan([item("a", "Drop")])).toMatch(/at least one/i);
		expect(validatePlan([item("a"), item("b", "Squash")])).toBeNull();
	});
});

function item(
	hash: string,
	action: InteractiveRebasePlanItem["action"] = "Pick",
	message = "message",
): InteractiveRebasePlanItem {
	return { action, hash, message };
}
