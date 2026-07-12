// @vitest-environment jsdom

import { fireEvent, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type {
	CommitGraphRow,
	CommitInfo,
	CommitRefInfo,
} from "@/generated/types";
import { RefCell } from "./RefCell";

const mocks = vi.hoisted(() => ({ normalizeRefs: vi.fn() }));
vi.mock("./RefCellUtils", async (importOriginal) => {
	const actual = await importOriginal<typeof import("./RefCellUtils")>();
	return {
		...actual,
		normalizeRefs: (...arguments_: Parameters<typeof actual.normalizeRefs>) => {
			mocks.normalizeRefs();
			return actual.normalizeRefs(...arguments_);
		},
	};
});

describe("RefCell", () => {
	beforeEach(() => vi.clearAllMocks());

	it("skips ref normalization and grouping for ordinary commits", () => {
		const value = row();
		value.commit.refs = [];
		render(<Cell row={value} />);

		expect(mocks.normalizeRefs).not.toHaveBeenCalled();
		expect(screen.queryByRole("button")).not.toBeInTheDocument();
	});

	it("reveals grouped refs on hover and preserves their context menus", async () => {
		const user = userEvent.setup();
		render(<Cell row={row()} />);

		const visibleRefGroup = screen.getByRole("button", {
			name: /Show 3 grouped references/,
		});
		expect(visibleRefGroup).toHaveTextContent("main");
		expect(visibleRefGroup).not.toHaveTextContent("+3");
		const hiddenCount = screen.getByText("+3");
		expect(hiddenCount).toBeVisible();
		await user.hover(hiddenCount);
		expect(screen.queryByTitle("origin/release")).not.toBeInTheDocument();
		await user.hover(visibleRefGroup);
		const remoteRef = await screen.findByTitle("origin/release");
		expect(remoteRef).not.toBeNull();
		if (!remoteRef) throw new Error("Remote ref was not rendered.");
		expect(remoteRef.closest(".custom-scrollbar")).not.toBeNull();

		fireEvent.contextMenu(remoteRef);
		expect(await screen.findByText("Check out as local branch…")).toBeVisible();
	});
});

function Cell({ row: value }: { row: CommitGraphRow }) {
	return (
		<RefCell
			branchMutationBusy={false}
			branchRemoteName="origin"
			currentBranchName="main"
			onBranchAction={vi.fn()}
			onCreateBranchFromTag={vi.fn()}
			onIntegrateBranch={vi.fn()}
			onTagAction={vi.fn()}
			remotePrefixes={["origin"]}
			row={value}
			tagMutationBusy={false}
			tagRemoteName="origin"
		/>
	);
}

function row(): CommitGraphRow {
	return {
		activeLanesAbove: [],
		activeLanesBelow: [],
		colorIndex: 0,
		commit: commit(),
		edgesAbove: [],
		edgesBelow: [],
		isBranchTip: true,
		isMergeCommit: false,
		lane: 0,
		laneColorsAbove: [],
		laneColorsBelow: [],
		rowIndex: 0,
	};
}

function commit(): CommitInfo {
	return {
		author: "Test",
		date: 0,
		email: "test@example.invalid",
		hash: "0123456789abcdef0123456789abcdef01234567",
		message: "Grouped refs",
		refs: [
			ref("Local", "main"),
			ref("Remote", "origin/main"),
			ref("Local", "feature/demo"),
			ref("Remote", "origin/release"),
			ref("Tag", "v1"),
		],
		signatureKind: "None",
		stats: null,
	};
}

function ref(kind: CommitRefInfo["kind"], name: string): CommitRefInfo {
	return { kind, name, remoteUrl: null };
}
