import { describe, expect, it } from "vitest";
import type {
	GitConflictStateResponse,
	GitOperationKind,
} from "@/generated/types";
import { isConflictOperationActive } from "./ConflictTransition";

describe("isConflictOperationActive", () => {
	it("only treats in-progress operations as active conflicts", () => {
		expect(isConflictOperationActive(state("Merge", true))).toBe(true);
		expect(isConflictOperationActive(state("Rebase", true))).toBe(true);
		expect(isConflictOperationActive(state("None", false))).toBe(false);
		expect(isConflictOperationActive(state("Merge", false))).toBe(false);
		expect(isConflictOperationActive(null)).toBe(false);
	});
});

function state(
	kind: GitOperationKind,
	isInProgress: boolean,
): GitConflictStateResponse {
	return {
		commitMessage: "",
		conflictedFiles: [],
		operation: {
			description: "",
			isInProgress,
			kind,
			label: "",
		},
		oursLabel: "",
		resolvedFiles: [],
		theirsLabel: "",
	};
}
