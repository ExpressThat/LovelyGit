// @vitest-environment jsdom

import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { AppOverlaysContainer } from "./AppOverlaysContainer";

vi.mock("./AppOverlays", () => ({
	AppOverlays: ({
		onBranchChanged,
	}: {
		onBranchChanged: (branchName: string) => void;
	}) => (
		<button onClick={() => onBranchChanged("feature/created")} type="button">
			Complete branch creation
		</button>
	),
}));

describe("AppOverlaysContainer", () => {
	it("refreshes the repository after command-palette branch creation", () => {
		const setCurrentBranchName = vi.fn();
		const onRepositoryChanged = vi.fn();
		render(
			<AppOverlaysContainer
				canCreateStash={false}
				currentBranchName="main"
				fileDiscovery={fileDiscovery()}
				onRefreshRepository={vi.fn()}
				onRepositoryChanged={onRepositoryChanged}
				overlays={overlays()}
				repositoryId="repository-id"
				setCurrentBranchName={setCurrentBranchName}
				setDetailsPanel={vi.fn()}
			/>,
		);

		fireEvent.click(
			screen.getByRole("button", { name: "Complete branch creation" }),
		);

		expect(setCurrentBranchName).toHaveBeenCalledWith("feature/created");
		expect(onRepositoryChanged).toHaveBeenCalledOnce();
	});
});

function overlays() {
	return {
		commandPaletteOpen: false,
		commitSearchOpen: false,
		createBranchOpen: true,
		remoteManagerOpen: false,
		resetRepositoryOverlays: vi.fn(),
		settingsOpen: false,
		stashOpen: false,
		setCommandPaletteOpen: vi.fn(),
		setCommitSearchOpen: vi.fn(),
		setCreateBranchOpen: vi.fn(),
		setRemoteManagerOpen: vi.fn(),
		setSettingsOpen: vi.fn(),
		setStashOpen: vi.fn(),
	};
}

function fileDiscovery() {
	return {
		blameTarget: null,
		closeBlame: vi.fn(),
		closeHistory: vi.fn(),
		historyTarget: null,
		openBlame: vi.fn(),
		openHistory: vi.fn(),
		reset: vi.fn(),
	};
}
