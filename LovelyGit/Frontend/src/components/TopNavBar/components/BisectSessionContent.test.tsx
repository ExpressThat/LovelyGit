// @vitest-environment jsdom
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { BisectSessionContent } from "./BisectSessionContent";

describe("BisectSessionContent", () => {
	it("marks the current revision bad and presents a discovered culprit", async () => {
		const user = userEvent.setup();
		const onRun = vi.fn();
		const view = render(
			<BisectSessionContent
				busyAction={null}
				isLoading={false}
				onRun={onRun}
				state={state()}
			/>,
		);

		await user.click(
			screen.getByRole("button", { name: "Mark current revision bad" }),
		);
		expect(onRun).toHaveBeenCalledWith("MarkBad");

		view.rerender(
			<BisectSessionContent
				busyAction={null}
				isLoading={false}
				onRun={onRun}
				state={state({ firstBadCommit: "d".repeat(40) })}
			/>,
		);
		await waitFor(() =>
			expect(screen.getByText("First bad commit found")).toBeVisible(),
		);
		expect(
			screen.getByRole("button", { name: "Reset bisect session" }),
		).toBeVisible();
	});
});

function state(overrides = {}) {
	return {
		badCommit: "b".repeat(40),
		currentCommit: "c".repeat(40),
		currentSubject: "Midpoint",
		firstBadCommit: null,
		goodCommits: ["a".repeat(40)],
		isActive: true,
		startingReference: "main",
		...overrides,
	};
}
