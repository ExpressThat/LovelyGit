// @vitest-environment jsdom

import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { TopNavBar } from "./TopNavBar";

vi.mock("../Settings/SettingsDialog", () => ({
	SettingsDialog: () => <button type="button">Settings</button>,
}));
vi.mock("./components/BranchControl", () => ({
	BranchControl: () => <div>Branch</div>,
}));
vi.mock("./components/RemoteActionsControl", () => ({
	RemoteActionsControl: () => <div>Remote</div>,
}));
vi.mock("./components/Tabs", () => ({ Tabs: () => <div>Tabs</div> }));
vi.mock("./components/TerminalActionControl", () => ({
	TerminalActionControl: () => <div>Terminal</div>,
}));

describe("TopNavBar", () => {
	it("opens commit search only when a repository is selected", async () => {
		const user = userEvent.setup();
		const onSearchCommits = vi.fn();
		const view = render(
			<TopNavBar
				currentBranchName="main"
				onBranchChanged={vi.fn()}
				onOpenCommandPalette={vi.fn()}
				onOpenWorkingChanges={vi.fn()}
				onRepositoryChanged={vi.fn()}
				onSearchCommits={onSearchCommits}
				repositoryId="repo"
				settingsOpen={false}
				onSettingsOpenChange={vi.fn()}
				workingChangesCount={0}
			/>,
		);
		const search = screen.getByRole("button", { name: "Search commits" });
		await user.click(search);
		expect(onSearchCommits).toHaveBeenCalledOnce();

		view.rerender(
			<TopNavBar
				currentBranchName={null}
				onBranchChanged={vi.fn()}
				onOpenCommandPalette={vi.fn()}
				onOpenWorkingChanges={vi.fn()}
				onRepositoryChanged={vi.fn()}
				onSearchCommits={onSearchCommits}
				repositoryId={null}
				settingsOpen={false}
				onSettingsOpenChange={vi.fn()}
				workingChangesCount={0}
			/>,
		);
		expect(search).toBeDisabled();
	});
});
