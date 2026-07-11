// @vitest-environment jsdom

import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { SubmoduleManager } from "./SubmoduleManager";

vi.mock("./SubmoduleManagerContent", () => ({
	SubmoduleManagerContent: () => <div>Submodule manager loaded</div>,
}));

describe("SubmoduleManager", () => {
	it("keeps its content unloaded until the user opens it", async () => {
		const user = userEvent.setup();
		render(<SubmoduleManager repositoryId="repo" />);
		expect(
			screen.queryByText("Submodule manager loaded"),
		).not.toBeInTheDocument();
		await user.click(screen.getByRole("button", { name: "Manage submodules" }));
		expect(await screen.findByText("Submodule manager loaded")).toBeVisible();
	});

	it("disables the trigger without a repository", () => {
		render(<SubmoduleManager repositoryId={null} />);
		expect(
			screen.getByRole("button", { name: "Manage submodules" }),
		).toBeDisabled();
	});
});
