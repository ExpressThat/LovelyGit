// @vitest-environment jsdom

import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import type { RepositoryStashItem } from "@/generated/types";
import { StashList } from "./StashList";

const stash: RepositoryStashItem = {
	commitHash: "0123456789abcdef",
	createdAtUnixSeconds: 1_700_000_000,
	message: "WIP on main",
	selector: "stash@{0}",
};

describe("StashList", () => {
	it("bounds rendering for large stash reflogs", () => {
		const stashes = Array.from({ length: 500 }, (_, index) => ({
			...stash,
			commitHash: index.toString(16).padStart(40, "0"),
			message: `Saved work ${index}`,
			selector: `stash@{${index}}`,
		}));
		render(
			<StashList
				busyAction={null}
				isLoading={false}
				loadError={null}
				onApply={vi.fn()}
				onBranch={vi.fn()}
				onDrop={vi.fn()}
				onInspect={vi.fn()}
				onPop={vi.fn()}
				stashes={stashes}
			/>,
		);

		expect(screen.getAllByRole("article")).toHaveLength(10);
		expect(screen.queryByText("Saved work 499")).toBeNull();
	});

	it("selects the exact stash for branch recovery", async () => {
		const user = userEvent.setup();
		const onBranch = vi.fn();
		const onInspect = vi.fn();
		render(
			<StashList
				busyAction={null}
				isLoading={false}
				loadError={null}
				onApply={vi.fn()}
				onBranch={onBranch}
				onDrop={vi.fn()}
				onInspect={onInspect}
				onPop={vi.fn()}
				stashes={[stash]}
			/>,
		);

		await user.click(screen.getByRole("button", { name: "Branch" }));
		expect(onBranch).toHaveBeenCalledWith(stash);
		await user.click(screen.getByRole("button", { name: "Inspect" }));
		expect(onInspect).toHaveBeenCalledWith(stash);
	});
});
