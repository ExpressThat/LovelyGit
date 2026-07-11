// @vitest-environment jsdom

import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { Dialog } from "@/components/ui/dialog";
import { SparseCheckoutManagerContent } from "./SparseCheckoutManagerContent";
import { useSparseCheckoutManager } from "./useSparseCheckoutManager";

vi.mock("./useSparseCheckoutManager", () => ({
	useSparseCheckoutManager: vi.fn(),
}));

describe("SparseCheckoutManagerContent", () => {
	beforeEach(() => {
		vi.clearAllMocks();
		vi.mocked(useSparseCheckoutManager).mockReturnValue(controller());
	});

	it("enables cone mode with one repository-relative directory per line", async () => {
		const user = userEvent.setup();
		const run = vi.fn().mockResolvedValue(true);
		vi.mocked(useSparseCheckoutManager).mockReturnValue(controller({ run }));
		renderContent();

		await user.type(screen.getByLabelText("Directories"), "src\napps/desktop");
		await user.click(
			screen.getByRole("button", { name: "Enable sparse checkout" }),
		);

		expect(run).toHaveBeenCalledWith("Set", true, ["src", "apps/desktop"]);
	});

	it("requires confirmation before restoring the full working tree", async () => {
		const user = userEvent.setup();
		const run = vi.fn().mockResolvedValue(true);
		vi.mocked(useSparseCheckoutManager).mockReturnValue(
			controller({
				run,
				state: { coneMode: true, enabled: true, patterns: ["src"] },
			}),
		);
		renderContent();

		await user.click(
			screen.getByRole("button", { name: "Restore full checkout" }),
		);
		expect(run).not.toHaveBeenCalled();
		expect(
			await screen.findByRole("heading", {
				name: "Restore the full working tree?",
			}),
		).toBeVisible();
		await user.click(screen.getByRole("button", { name: "Restore all files" }));

		expect(run).toHaveBeenCalledWith("Disable", false, []);
	});

	it("supports non-cone Git patterns", async () => {
		const user = userEvent.setup();
		const run = vi.fn().mockResolvedValue(true);
		vi.mocked(useSparseCheckoutManager).mockReturnValue(controller({ run }));
		renderContent();

		await user.click(screen.getByRole("switch", { name: "Cone mode" }));
		await user.type(
			screen.getByLabelText("Git ignore-style patterns"),
			"/*\n!/vendor/",
		);
		await user.click(
			screen.getByRole("button", { name: "Enable sparse checkout" }),
		);

		expect(run).toHaveBeenCalledWith("Set", false, ["/*", "!/vendor/"]);
	});

	it("locks controls during mutation", () => {
		vi.mocked(useSparseCheckoutManager).mockReturnValue(
			controller({ busyAction: "Set" }),
		);
		renderContent();

		expect(screen.getByLabelText("Directories")).toBeDisabled();
		expect(
			screen.getByRole("button", { name: "Enable sparse checkout" }),
		).toBeDisabled();
	});
});

function renderContent() {
	return render(
		<Dialog open>
			<SparseCheckoutManagerContent repositoryId="repo" />
		</Dialog>,
	);
}

function controller(overrides: Record<string, unknown> = {}) {
	return {
		busyAction: null,
		error: null,
		isLoading: false,
		load: vi.fn().mockResolvedValue(undefined),
		run: vi.fn().mockResolvedValue(true),
		state: { coneMode: false, enabled: false, patterns: [] },
		...overrides,
	} as ReturnType<typeof useSparseCheckoutManager>;
}
