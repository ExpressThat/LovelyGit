// @vitest-environment jsdom

import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { CommitMultiSelectionBar } from "./CommitMultiSelectionBar";

describe("CommitMultiSelectionBar", () => {
	it("offers comparison only for exactly two selected commits", async () => {
		const onCompare = vi.fn();
		const { rerender } = renderBar(1, onCompare);
		expect(screen.queryByRole("button", { name: "Compare" })).toBeNull();

		rerender(<Harness count={2} onCompare={onCompare} />);
		await userEvent.click(screen.getByRole("button", { name: "Compare" }));
		expect(onCompare).toHaveBeenCalledOnce();

		rerender(<Harness count={3} onCompare={onCompare} />);
		expect(screen.queryByRole("button", { name: "Compare" })).toBeNull();
	});

	it("offers copy and save patch-series actions for multiple commits", async () => {
		const onCopy = vi.fn();
		const onSave = vi.fn();
		render(
			<Harness count={3} onCompare={vi.fn()} onCopy={onCopy} onSave={onSave} />,
		);

		await userEvent.click(screen.getByRole("button", { name: "Patch series" }));
		expect(
			await screen.findByText("3 commits · oldest to newest"),
		).toBeVisible();
		await userEvent.click(screen.getByText("Copy patch series"));
		expect(onCopy).toHaveBeenCalledOnce();

		await userEvent.click(screen.getByRole("button", { name: "Patch series" }));
		await userEvent.click(await screen.findByText("Save patch series…"));
		expect(onSave).toHaveBeenCalledOnce();
	});
});

function renderBar(count: number, onCompare: () => void) {
	return render(<Harness count={count} onCompare={onCompare} />);
}

function Harness({
	count,
	onCompare,
	onCopy = vi.fn(),
	onSave = vi.fn(),
}: {
	count: number;
	onCompare: () => void;
	onCopy?: () => void;
	onSave?: () => void;
}) {
	return (
		<CommitMultiSelectionBar
			cherryPickDisabled={false}
			count={count}
			onCherryPick={vi.fn()}
			onClear={vi.fn()}
			onCompare={onCompare}
			onCopyPatchSeries={onCopy}
			onRevert={vi.fn()}
			onSavePatchSeries={onSave}
			revertDisabled={false}
			seriesBusyAction={null}
		/>
	);
}
