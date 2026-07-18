// @vitest-environment jsdom

import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { RepositoryRefItem } from "@/generated/types";
import { BranchMenuContent } from "./BranchMenuContent";

vi.mock("./VirtualBranchMenuList", () => ({
	VirtualBranchMenuList: () => <div id="branch-switcher-results">Branches</div>,
}));

vi.mock("@/components/ui/dropdown-menu", () => ({
	DropdownMenuGroup: ({ children }: React.PropsWithChildren) => (
		<div>{children}</div>
	),
	DropdownMenuLabel: ({ children }: React.PropsWithChildren) => (
		<div>{children}</div>
	),
	DropdownMenuSeparator: () => <hr />,
}));

const branches: RepositoryRefItem[] = ["main", "topic", "release"].map(
	(name) => ({ commitHash: "abc", kind: "Local", name, remoteUrl: null }),
);

describe("BranchMenuContent", () => {
	it("filters from native input events", () => {
		const onQueryChange = vi.fn();
		renderMenu({
			activeIndex: 0,
			onActiveIndexChange: vi.fn(),
			onCheckout: vi.fn(),
			onQueryChange,
		});

		fireEvent.input(screen.getByRole("textbox"), {
			target: { value: "topic" },
		});
		expect(onQueryChange).toHaveBeenCalledWith("topic");
	});

	it("navigates every filtered branch from the search field", () => {
		const onActiveIndexChange = vi.fn();
		const onCheckout = vi.fn();
		renderMenu({ activeIndex: 0, onActiveIndexChange, onCheckout });
		const input = screen.getByRole("textbox", { name: "Filter branches" });

		fireEvent.keyDown(input, { key: "ArrowUp" });
		expect(onActiveIndexChange).toHaveBeenCalledWith(2);
		fireEvent.keyDown(input, { key: "End" });
		expect(onActiveIndexChange).toHaveBeenCalledWith(2);
	});

	it("checks out the active branch with Enter", () => {
		const onCheckout = vi.fn();
		renderMenu({ activeIndex: 1, onActiveIndexChange: vi.fn(), onCheckout });

		fireEvent.keyDown(screen.getByRole("textbox"), { key: "Enter" });
		expect(onCheckout).toHaveBeenCalledWith("topic");
	});
});

function renderMenu({
	activeIndex,
	onActiveIndexChange,
	onCheckout,
	onQueryChange = vi.fn(),
}: {
	activeIndex: number;
	onActiveIndexChange: (index: number) => void;
	onCheckout: (branchName: string) => void;
	onQueryChange?: (query: string) => void;
}) {
	return render(
		<BranchMenuContent
			activeIndex={activeIndex}
			branches={branches}
			busy={false}
			currentBranchName="main"
			error={null}
			isLoading={false}
			onActiveIndexChange={onActiveIndexChange}
			onCheckout={onCheckout}
			onQueryChange={onQueryChange}
			query=""
		/>,
	);
}
