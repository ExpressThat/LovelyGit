// @vitest-environment jsdom

import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { CommitParentSelector } from "./CommitParentSelector";

const parents = ["a".repeat(40), "b".repeat(40)];

describe("CommitParentSelector", () => {
	it("describes and selects either merge parent", async () => {
		const onChange = vi.fn();
		const user = userEvent.setup();
		render(
			<CommitParentSelector
				busy={false}
				onChange={onChange}
				parents={parents}
				selectedIndex={0}
			/>,
		);

		expect(
			screen.getByRole("button", { name: "Parent 1 · aaaaaaa" }),
		).toHaveAttribute("aria-pressed", "true");
		await user.click(
			screen.getByRole("button", { name: "Parent 2 · bbbbbbb" }),
		);
		expect(onChange).toHaveBeenCalledWith(1);
	});

	it("stays absent for ordinary commits and locks during refresh", () => {
		const { rerender } = render(
			<CommitParentSelector
				busy={false}
				onChange={vi.fn()}
				parents={[parents[0]]}
				selectedIndex={0}
			/>,
		);
		expect(
			screen.queryByRole("group", { name: "Merge commit parent" }),
		).not.toBeInTheDocument();

		rerender(
			<CommitParentSelector
				busy
				onChange={vi.fn()}
				parents={parents}
				selectedIndex={1}
			/>,
		);
		for (const button of screen.getAllByRole("button")) {
			expect(button).toBeDisabled();
		}
	});
});
