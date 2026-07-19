// @vitest-environment jsdom

import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { ComponentProps, ReactNode } from "react";
import { beforeEach, expect, it, vi } from "vitest";
import { BranchControl } from "./BranchControl";

const send = vi.hoisted(() => vi.fn(async () => undefined));
const waitForPaint = vi.hoisted(() => vi.fn<() => Promise<void>>());

vi.mock("@/lib/commands", () => ({ sendRequestWithResponse: send }));
vi.mock("@/lib/waitForBrowserPaint", () => ({
	waitForBrowserPaint: waitForPaint,
}));
vi.mock("sonner", () => ({
	toast: {
		error: vi.fn(),
		loading: vi.fn(() => "toast-1"),
		success: vi.fn(),
	},
}));
vi.mock("@/components/ui/dropdown-menu", async () => {
	const { createElement, Fragment } = await import("react");
	return {
		DropdownMenu: ({ children }: { children: ReactNode }) =>
			createElement(Fragment, null, children),
		DropdownMenuContent: ({ children }: { children: ReactNode }) =>
			createElement("div", null, children),
		DropdownMenuGroup: ({ children }: { children: ReactNode }) =>
			createElement("div", null, children),
		DropdownMenuItem: ({ children, ...props }: ComponentProps<"button">) =>
			createElement("button", { ...props, role: "menuitem" }, children),
		DropdownMenuLabel: ({ children }: { children: ReactNode }) =>
			createElement("div", null, children),
		DropdownMenuSeparator: () => createElement("hr"),
		DropdownMenuTrigger: (props: ComponentProps<"button">) =>
			createElement("button", props),
	};
});
vi.mock("./useLocalBranches", () => ({
	useLocalBranches: () => ({
		branches: [
			{ hash: "1111111", name: "main" },
			{ hash: "2222222", name: "feature" },
		],
		error: null,
		isLoading: false,
	}),
}));
vi.mock("./LazyRepositoryDialogs", () => ({
	LazyBranchIntegrationDialog: () => null,
	LazyCreateBranchDialog: () => null,
}));

beforeEach(() => {
	send.mockReset();
	waitForPaint.mockReset();
});

it("paints the disabled branch selector before entering the native bridge", async () => {
	const user = userEvent.setup();
	let finishPaint: (() => void) | undefined;
	waitForPaint.mockReturnValueOnce(
		new Promise<void>((resolve) => {
			finishPaint = resolve;
		}),
	);
	render(
		<BranchControl
			currentBranchName="main"
			onBranchChanged={vi.fn()}
			onOpenWorkingChanges={vi.fn()}
			onRepositoryChanged={vi.fn()}
			repositoryId="repo-1"
		/>,
	);

	await user.click(screen.getByRole("menuitem", { name: "feature" }));
	expect(screen.getByRole("button", { name: "Switch branch" })).toBeDisabled();
	expect(send).not.toHaveBeenCalled();

	finishPaint?.();
	await waitFor(() => expect(send).toHaveBeenCalledOnce());
});

it("re-enables branch switching after failure and permits retry", async () => {
	const user = userEvent.setup();
	const onBranchChanged = vi.fn();
	waitForPaint.mockResolvedValue(undefined);
	send.mockRejectedValueOnce(new Error("Working tree is dirty"));
	render(
		<BranchControl
			currentBranchName="main"
			onBranchChanged={onBranchChanged}
			onOpenWorkingChanges={vi.fn()}
			onRepositoryChanged={vi.fn()}
			repositoryId="repo-1"
		/>,
	);

	await user.click(screen.getByRole("menuitem", { name: "feature" }));
	await waitFor(() =>
		expect(screen.getByRole("button", { name: "Switch branch" })).toBeEnabled(),
	);
	expect(onBranchChanged).not.toHaveBeenCalled();

	send.mockResolvedValueOnce(undefined);
	await user.click(screen.getByRole("menuitem", { name: "feature" }));
	await waitFor(() => expect(onBranchChanged).toHaveBeenCalledWith("feature"));
});
