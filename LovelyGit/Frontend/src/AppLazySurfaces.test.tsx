// @vitest-environment jsdom
import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { CommitDetailsSurface } from "./AppLazySurfaces";

vi.mock("./components/CommitDetails/CommitDetails", () => ({
	CommitDetails: () => <div>Loaded commit details</div>,
}));

describe("AppLazySurfaces", () => {
	it("renders commit details synchronously without a suspense delay", () => {
		render(
			<CommitDetailsSurface
				commitHash={"a".repeat(40)}
				onOpenFileBlame={vi.fn()}
				onOpenFileHistory={vi.fn()}
				onParentIndexChange={vi.fn()}
				onSelectFile={vi.fn()}
				parentIndex={0}
				repositoryId="repo"
			/>,
		);

		expect(screen.getByText("Loaded commit details")).toBeVisible();
	});
});
