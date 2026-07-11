// @vitest-environment jsdom

import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { LfsManager } from "./LfsManager";

vi.mock("./LfsManagerContent", () => ({
	LfsManagerContent: () => <div>Git LFS manager loaded</div>,
}));

describe("LfsManager", () => {
	it("keeps the manager unloaded until its trigger opens", async () => {
		const user = userEvent.setup();
		render(<LfsManager repositoryId="repo" />);
		expect(
			screen.queryByText("Git LFS manager loaded"),
		).not.toBeInTheDocument();

		await user.click(screen.getByRole("button", { name: "Manage Git LFS" }));

		expect(await screen.findByText("Git LFS manager loaded")).toBeVisible();
	});

	it("disables the trigger until a repository is selected", () => {
		render(<LfsManager repositoryId={null} />);
		expect(
			screen.getByRole("button", { name: "Manage Git LFS" }),
		).toBeDisabled();
	});
});
