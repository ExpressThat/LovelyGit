// @vitest-environment jsdom

import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import {
	LazyBranchIntegrationDialog,
	LazyCreateBranchDialog,
	LazyRemoteManagerDialog,
} from "./LazyRepositoryDialogs";

vi.mock("./CreateBranchDialog", () => ({
	CreateBranchDialog: () => <div>Create branch loaded</div>,
}));
vi.mock("./BranchIntegrationDialog", () => ({
	BranchIntegrationDialog: () => <div>Branch integration loaded</div>,
}));
vi.mock("./RemoteManagerDialog", () => ({
	RemoteManagerDialog: () => <div>Remote manager loaded</div>,
}));

describe("LazyRepositoryDialogs", () => {
	it("does not load closed repository dialogs", () => {
		render(
			<>
				<LazyCreateBranchDialog {...createBranchProps()} open={false} />
				<LazyBranchIntegrationDialog
					{...branchIntegrationProps()}
					mode={null}
				/>
				<LazyRemoteManagerDialog {...remoteManagerProps()} open={false} />
			</>,
		);

		expect(screen.queryByText(/loaded/)).not.toBeInTheDocument();
	});

	it("reveals each repository dialog without Suspense retention", async () => {
		const view = render(
			<LazyCreateBranchDialog {...createBranchProps()} open />,
		);
		expect(await screen.findByText("Create branch loaded")).toBeVisible();

		view.rerender(
			<LazyBranchIntegrationDialog
				{...branchIntegrationProps()}
				mode="merge"
			/>,
		);
		expect(await screen.findByText("Branch integration loaded")).toBeVisible();

		view.rerender(<LazyRemoteManagerDialog {...remoteManagerProps()} open />);
		expect(await screen.findByText("Remote manager loaded")).toBeVisible();
	});
});

function createBranchProps() {
	return {
		currentBranchName: "main",
		onBranchChanged: vi.fn(),
		onOpenChange: vi.fn(),
		onRepositoryChanged: vi.fn(),
		repositoryId: "repo",
	};
}

function branchIntegrationProps() {
	return {
		branches: [],
		currentBranchName: "main",
		onOpenWorkingChanges: vi.fn(),
		onRepositoryChanged: vi.fn(),
		onOpenChange: vi.fn(),
		repositoryId: "repo",
	};
}

function remoteManagerProps() {
	return {
		onOpenChange: vi.fn(),
		repositoryId: "repo",
	};
}
