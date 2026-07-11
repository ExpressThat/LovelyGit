// @vitest-environment jsdom

import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { Dialog } from "@/components/ui/dialog";
import { LfsManagerContent } from "./LfsManagerContent";

const manager = vi.hoisted(() => ({
	busyAction: null as null | string,
	busyPattern: null as null | string,
	error: null as null | string,
	isLoading: false,
	load: vi.fn(),
	run: vi.fn().mockResolvedValue(true),
	state: {
		hasTrackedPatterns: true,
		isAvailable: true,
		isInitialized: true,
		trackedPatterns: ["*.psd"],
	},
}));

vi.mock("./useLfsManager", () => ({ useLfsManager: () => manager }));

describe("LfsManagerContent", () => {
	beforeEach(() => {
		vi.clearAllMocks();
		manager.state.isAvailable = true;
		manager.state.isInitialized = true;
		manager.state.trackedPatterns = ["*.psd"];
	});

	it("tracks and untracks explicit patterns", async () => {
		const user = userEvent.setup();
		renderContent();
		expect(screen.getByText("Git LFS is ready")).toBeVisible();
		expect(
			screen.getByRole("button", { name: "Reinstall hooks" }),
		).toBeEnabled();

		await user.type(screen.getByLabelText("LFS path pattern"), "Assets/**");
		await user.click(screen.getByRole("button", { name: "Track pattern" }));
		expect(manager.run).toHaveBeenCalledWith("Track", "Assets/**");

		await user.click(
			screen.getByRole("button", {
				name: "Stop tracking *.psd with Git LFS",
			}),
		);
		expect(manager.run).toHaveBeenCalledWith("Untrack", "*.psd");
	});

	it("requires confirmation before pruning and cancellation does not mutate", async () => {
		const user = userEvent.setup();
		renderContent();

		await user.click(screen.getByRole("button", { name: "Prune cache" }));
		expect(
			await screen.findByRole("alertdialog", {
				name: "Prune the local LFS cache?",
			}),
		).toBeVisible();
		await user.click(screen.getByRole("button", { name: "Cancel" }));
		expect(manager.run).not.toHaveBeenCalled();

		await user.click(screen.getByRole("button", { name: "Prune cache" }));
		const confirmation = await screen.findByRole("alertdialog", {
			name: "Prune the local LFS cache?",
		});
		await user.click(
			within(confirmation).getByRole("button", { name: "Prune cache" }),
		);
		expect(manager.run).toHaveBeenCalledWith("Prune");
	});

	it("explains unavailable LFS and disables every mutation", () => {
		manager.state.isAvailable = false;
		renderContent();

		expect(screen.getByText("Git LFS is unavailable")).toBeVisible();
		expect(screen.getByRole("button", { name: "Initialize" })).toBeDisabled();
		expect(
			screen.getByRole("button", { name: "Fetch objects" }),
		).toBeDisabled();
		expect(screen.getByRole("button", { name: "Pull objects" })).toBeDisabled();
		expect(screen.getByRole("button", { name: "Prune cache" })).toBeDisabled();
		expect(screen.getByLabelText("LFS path pattern")).toBeDisabled();
	});
});

function renderContent() {
	return render(
		<Dialog open>
			<LfsManagerContent repositoryId="repo" />
		</Dialog>,
	);
}
