// @vitest-environment jsdom

import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { SparseCheckoutManager } from "./SparseCheckoutManager";

vi.mock("./SparseCheckoutManagerContent", () => ({
	SparseCheckoutManagerContent: () => <div>Sparse checkout manager loaded</div>,
}));

describe("SparseCheckoutManager", () => {
	it("loads its content only after the user opens it", async () => {
		const user = userEvent.setup();
		render(<SparseCheckoutManager repositoryId="repo" />);
		expect(
			screen.queryByText("Sparse checkout manager loaded"),
		).not.toBeInTheDocument();
		await user.click(
			screen.getByRole("button", { name: "Manage sparse checkout" }),
		);
		expect(
			await screen.findByText("Sparse checkout manager loaded"),
		).toBeVisible();
	});

	it("disables the trigger without a repository", () => {
		render(<SparseCheckoutManager repositoryId={null} />);
		expect(
			screen.getByRole("button", { name: "Manage sparse checkout" }),
		).toBeDisabled();
	});
});
