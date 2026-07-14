// @vitest-environment jsdom

import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { StashScopeControl } from "./StashScopeControl";

describe("StashScopeControl", () => {
	it("makes selected files the explicit active scope", async () => {
		const onChange = vi.fn();
		render(
			<StashScopeControl
				onSelectedOnlyChange={onChange}
				selectedCount={2}
				selectedOnly
			/>,
		);

		expect(
			screen.getByRole("button", { name: "Selected files (2)" }),
		).toHaveAttribute("aria-pressed", "true");
		expect(
			screen.getByText(
				"Git will stash staged and unstaged changes for each selected path.",
			),
		).toBeVisible();

		await userEvent.click(screen.getByRole("button", { name: "All changes" }));
		expect(onChange).toHaveBeenCalledWith(false);
	});

	it("does not offer an empty selected-file scope", () => {
		render(
			<StashScopeControl
				onSelectedOnlyChange={vi.fn()}
				selectedCount={0}
				selectedOnly={false}
			/>,
		);

		expect(
			screen.getByRole("button", { name: "Selected files (0)" }),
		).toBeDisabled();
	});
});
