// @vitest-environment jsdom

import { fireEvent, render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { Dialog } from "@/components/ui/dialog";
import { SubmoduleManagerContent } from "./SubmoduleManagerContent";

const manager = vi.hoisted(() => ({
	busyPath: null as string | null,
	error: null as string | null,
	isLoading: false,
	load: vi.fn(),
	run: vi.fn(),
	submodules: Array.from({ length: 500 }, (_, index) => ({
		branch: "main",
		currentCommit: index.toString(16).padStart(40, "0"),
		expectedCommit: index.toString(16).padStart(40, "0"),
		name: `module-${index}`,
		path: `modules/${index}`,
		state: "Current" as const,
		url: `https://example.invalid/${index}`,
	})),
}));

vi.mock("./useSubmoduleManager", () => ({
	useSubmoduleManager: () => manager,
}));

describe("SubmoduleManagerContent", () => {
	beforeEach(() => vi.clearAllMocks());

	it("bounds large configurations and preserves row actions", () => {
		render(
			<Dialog open>
				<SubmoduleManagerContent repositoryId="repo" />
			</Dialog>,
		);

		expect(screen.getAllByRole("article")).toHaveLength(10);
		expect(screen.queryByText("module-499")).toBeNull();
		fireEvent.click(screen.getAllByRole("button", { name: "Update" })[0]);
		expect(manager.run).toHaveBeenCalledWith("modules/0", "Update");
	});
});
