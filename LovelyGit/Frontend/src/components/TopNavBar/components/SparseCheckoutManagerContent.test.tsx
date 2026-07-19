// @vitest-environment jsdom

import { fireEvent, render, screen, waitFor } from "@testing-library/react";
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

		expect(run).toHaveBeenCalledWith("Set", true, "src\napps/desktop");
	});

	it("requires confirmation before restoring the full working tree", async () => {
		const user = userEvent.setup();
		const run = vi.fn().mockResolvedValue(true);
		vi.mocked(useSparseCheckoutManager).mockReturnValue(
			controller({
				run,
				state: {
					coneMode: true,
					enabled: true,
					patternCount: 1,
					patternText: "src",
					patternTextGzipBase64: "",
				},
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

		expect(run).toHaveBeenCalledWith("Disable", false, "");
		await waitFor(() =>
			expect(
				screen.queryByRole("heading", {
					name: "Restore the full working tree?",
				}),
			).not.toBeInTheDocument(),
		);
	});

	it("keeps restore confirmation open after failure for retry", async () => {
		const user = userEvent.setup();
		const run = vi.fn().mockResolvedValue(false);
		vi.mocked(useSparseCheckoutManager).mockReturnValue(
			controller({
				run,
				state: {
					coneMode: true,
					enabled: true,
					patternCount: 1,
					patternText: "src",
					patternTextGzipBase64: "",
				},
			}),
		);
		renderContent();

		await user.click(
			screen.getByRole("button", { name: "Restore full checkout" }),
		);
		await user.click(screen.getByRole("button", { name: "Restore all files" }));

		expect(run).toHaveBeenCalledWith("Disable", false, "");
		expect(
			screen.getByRole("heading", {
				name: "Restore the full working tree?",
			}),
		).toBeVisible();
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

		expect(run).toHaveBeenCalledWith("Set", false, "/*\n!/vendor/");
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

	it("keeps a large specification compact and editable when applied", async () => {
		const user = userEvent.setup();
		const patterns = Array.from(
			{ length: 100_000 },
			(_, index) => `path-${index}`,
		);
		const run = vi.fn().mockResolvedValue(true);
		vi.mocked(useSparseCheckoutManager).mockReturnValue(
			controller({
				run,
				state: {
					coneMode: false,
					enabled: true,
					patternCount: patterns.length,
					patternText: patterns.join("\n"),
					patternTextGzipBase64: "",
				},
			}),
		);
		renderContent();
		const editor = screen.getByRole("textbox") as HTMLTextAreaElement;

		fireEvent.input(editor, { target: { value: `${editor.value}\nextra` } });
		await user.click(screen.getByRole("button", { name: "Apply selection" }));

		const applied = run.mock.calls[0]?.[2] as string;
		expect(applied).toMatch(/^path-0\n/);
		expect(applied).toMatch(/path-99999\nextra$/);
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
		state: {
			coneMode: false,
			enabled: false,
			patternCount: 0,
			patternText: "",
			patternTextGzipBase64: "",
		},
		...overrides,
	} as ReturnType<typeof useSparseCheckoutManager>;
}
