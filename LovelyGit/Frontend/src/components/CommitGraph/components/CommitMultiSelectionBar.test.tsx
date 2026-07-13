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
});

function renderBar(count: number, onCompare: () => void) {
	return render(<Harness count={count} onCompare={onCompare} />);
}

function Harness({
	count,
	onCompare,
}: {
	count: number;
	onCompare: () => void;
}) {
	return (
		<CommitMultiSelectionBar
			cherryPickDisabled={false}
			count={count}
			onCherryPick={vi.fn()}
			onClear={vi.fn()}
			onCompare={onCompare}
			onRevert={vi.fn()}
			revertDisabled={false}
		/>
	);
}
