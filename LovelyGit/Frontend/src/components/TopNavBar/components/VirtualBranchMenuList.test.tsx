// @vitest-environment jsdom

import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { RepositoryRefItem } from "@/generated/types";
import { VirtualBranchMenuList } from "./VirtualBranchMenuList";

const virtualizerInput = vi.hoisted(() => vi.fn());
const scrollToIndex = vi.hoisted(() => vi.fn());

vi.mock("@tanstack/react-virtual", () => ({
	useVirtualizer: (input: { count: number }) => {
		virtualizerInput(input);
		const renderedCount = Math.min(input.count, 8);
		return {
			getTotalSize: () => input.count * 32,
			getVirtualItems: () =>
				Array.from({ length: renderedCount }, (_, index) => ({
					index,
					start: index * 32,
				})),
			scrollToIndex,
		};
	},
}));

vi.mock("@/components/ui/dropdown-menu", () => ({
	DropdownMenuItem: ({
		children,
		...props
	}: React.ComponentProps<"button">) => (
		<button type="button" {...props}>
			{children}
		</button>
	),
}));

describe("VirtualBranchMenuList", () => {
	it("mounts only a small window for a large branch collection", () => {
		const onCheckout = vi.fn();
		render(
			<VirtualBranchMenuList
				activeIndex={0}
				branches={Array.from({ length: 10_000 }, (_, index) => branch(index))}
				busy={false}
				currentBranchName="branch-0"
				onActiveIndexChange={vi.fn()}
				onCheckout={onCheckout}
			/>,
		);

		expect(virtualizerInput).toHaveBeenCalledWith(
			expect.objectContaining({ count: 10_000, overscan: 6 }),
		);
		expect(screen.getAllByRole("button")).toHaveLength(8);
		expect(screen.queryByText("branch-9999")).not.toBeInTheDocument();
		fireEvent.click(screen.getByText("branch-3"));
		expect(onCheckout).toHaveBeenCalledWith("branch-3");
	});

	it("keeps the keyboard-active branch in view", () => {
		render(
			<VirtualBranchMenuList
				activeIndex={7}
				branches={Array.from({ length: 20 }, (_, index) => branch(index))}
				busy={false}
				currentBranchName={null}
				onActiveIndexChange={vi.fn()}
				onCheckout={vi.fn()}
			/>,
		);

		expect(scrollToIndex).toHaveBeenCalledWith(7, { align: "auto" });
	});
});

function branch(index: number): RepositoryRefItem {
	return {
		commitHash: index.toString(16),
		kind: "Local",
		name: `branch-${index}`,
		remoteUrl: null,
	};
}
