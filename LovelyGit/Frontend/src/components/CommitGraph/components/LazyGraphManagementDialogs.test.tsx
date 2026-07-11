// @vitest-environment jsdom

import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { LazyDeleteTagDialog } from "./LazyGraphManagementDialogs";

vi.mock("./DeleteTagDialog", () => ({
	DeleteTagDialog: ({ tagName }: { tagName: string | null }) =>
		tagName ? <div>Delete tag dialog loaded</div> : null,
}));

describe("LazyGraphManagementDialogs", () => {
	it("loads a management dialog only after its target becomes active", async () => {
		const props = {
			isBusy: false,
			onConfirm: vi.fn(),
			onOpenChange: vi.fn(),
			tagName: null,
		};
		const view = render(<LazyDeleteTagDialog {...props} />);
		expect(
			screen.queryByText("Delete tag dialog loaded"),
		).not.toBeInTheDocument();
		view.rerender(<LazyDeleteTagDialog {...props} tagName="v1.0.0" />);
		expect(await screen.findByText("Delete tag dialog loaded")).toBeVisible();
	});
});
