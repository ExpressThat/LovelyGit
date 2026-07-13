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
