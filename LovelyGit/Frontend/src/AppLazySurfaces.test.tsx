// @vitest-environment jsdom
import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { CommitDetailsSurface } from "./AppLazySurfaces";

vi.mock("./components/CommitDetails/CommitDetails", () => ({
	CommitDetails: () => <div>Loaded commit details</div>,
}));

describe("AppLazySurfaces", () => {
	it("loads commit details on demand through its suspense boundary", async () => {
		render(
			<CommitDetailsSurface
				commitHash={"a".repeat(40)}
				onOpenFileBlame={vi.fn()}
				onOpenFileHistory={vi.fn()}
				onSelectFile={vi.fn()}
				repositoryId="repo"
			/>,
		);

		expect(await screen.findByText("Loaded commit details")).toBeVisible();
	});
});
