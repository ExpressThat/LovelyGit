import { describe, expect, it, vi } from "vitest";
import type { CommitGraphRow } from "@/generated/types";
import { createSelectedCommitActions } from "./useSelectedCommitActions";

describe("createSelectedCommitActions", () => {
	it("opens a comparison and clears the deliberate selection", () => {
		const fixture = createFixture();
		fixture.selection.comparison.mockReturnValue({
			base: rows[1],
			target: rows[0],
		});

		fixture.actions.openComparison();

		expect(fixture.dialogs.comparison.setBase).toHaveBeenCalledWith(rows[1]);
		expect(fixture.dialogs.comparison.setTarget).toHaveBeenCalledWith(rows[0]);
		expect(fixture.selection.clear).toHaveBeenCalledOnce();
	});

	it("clears a copied patch series only after success", async () => {
		const fixture = createFixture();
		fixture.patchActions.copyPatchSeries.mockResolvedValue(true);

		fixture.actions.runPatchSeries("copy");
		await vi.waitFor(() =>
			expect(fixture.selection.clear).toHaveBeenCalledOnce(),
		);
		expect(fixture.patchActions.copyPatchSeries).toHaveBeenCalledWith(rows);
	});

	it("preserves selection when patch-series export fails", async () => {
		const fixture = createFixture();
		fixture.patchActions.savePatchSeries.mockResolvedValue(false);

		fixture.actions.runPatchSeries("save");
		await vi.waitFor(() =>
			expect(fixture.patchActions.savePatchSeries).toHaveBeenCalled(),
		);
		expect(fixture.selection.clear).not.toHaveBeenCalled();
	});
});

function createFixture() {
	const dialogs = {
		comparison: { setBase: vi.fn(), setTarget: vi.fn() },
		setCherryPickCommits: vi.fn(),
		setRevertCommits: vi.fn(),
	};
	const patchActions = {
		copyPatchSeries: vi.fn(),
		savePatchSeries: vi.fn(),
	};
	const selection = {
		clear: vi.fn(),
		comparison: vi.fn(),
		ordered: vi.fn().mockReturnValue(rows),
	};
	return {
		actions: createSelectedCommitActions({
			dialogs: dialogs as never,
			patchActions: patchActions as never,
			selection: selection as never,
		}),
		dialogs,
		patchActions,
		selection,
	};
}

const rows = ["1", "2"].map(
	(hash) =>
		({ commit: { hash: hash.repeat(40), message: hash } }) as CommitGraphRow,
);
