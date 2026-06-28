import { describe, expect, it } from "vitest";
import { getWorkingChangedFileRowActions } from "./WorkingChangedFileRowActions";

describe("getWorkingChangedFileRowActions", () => {
	it("hides mutating actions while the panel is busy", () => {
		expect(
			getWorkingChangedFileRowActions({
				canDiscard: true,
				canStage: true,
				canUnstage: true,
				isBusy: true,
			}),
		).toEqual([]);
	});

	it("shows only actions that apply to the file row", () => {
		expect(
			getWorkingChangedFileRowActions({
				canDiscard: true,
				canStage: true,
				canUnstage: false,
				isBusy: false,
			}),
		).toEqual(["stage", "discard"]);
	});
});
