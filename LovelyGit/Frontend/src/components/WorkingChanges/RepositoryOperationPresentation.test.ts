import { describe, expect, it } from "vitest";
import { repositoryOperationLabel } from "./RepositoryOperationPresentation";

describe("repositoryOperationLabel", () => {
	it.each([
		["CherryPick", "Cherry-pick"],
		["Merge", "Merge"],
		["Rebase", "Rebase"],
		["Revert", "Revert"],
	] as const)("formats %s", (operation, expected) => {
		expect(repositoryOperationLabel(operation)).toBe(expected);
	});
});
