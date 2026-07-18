// @vitest-environment jsdom

import { fireEvent, render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { KnownGitRepository } from "@/generated/types";
import { RecentRepositories } from "./RecentRepositories";

const state = vi.hoisted(() => ({
	closeRepository: vi.fn(),
	repositories: [] as KnownGitRepository[],
	setCurrentRepositoryId: vi.fn(),
}));
vi.mock("@/lib/repositoryContext", () => ({
	useRepositoryContext: () => ({
		closeRepository: state.closeRepository,
		repositories: state.repositories,
		setCurrentRepositoryId: state.setCurrentRepositoryId,
	}),
}));

describe("RecentRepositories", () => {
	beforeEach(() => {
		state.repositories = repositories(100);
		vi.clearAllMocks();
	});

	it("bounds a large repository collection", () => {
		const view = render(<RecentRepositories />);
		const rows = view.container.querySelectorAll("[data-repository-row]");
		expect(
			view.container.querySelector("[data-recent-repositories='virtual']"),
		).toBeInTheDocument();
		expect(rows.length).toBeLessThanOrEqual(12);
	});

	it("filters before presenting repository rows", () => {
		render(<RecentRepositories />);
		fireEvent.change(
			screen.getByRole("textbox", { name: "Search repositories" }),
			{
				target: { value: "repository-0099" },
			},
		);

		expect(screen.getByText("1 of 100")).toBeInTheDocument();
		fireEvent.click(screen.getByTitle("C:\\perf\\repo-0099"));
		expect(state.setCurrentRepositoryId).toHaveBeenCalledWith("99");
	});
});

function repositories(count: number): KnownGitRepository[] {
	return Array.from({ length: count }, (_, index) => ({
		id: `${index}`,
		name: `repository-${index.toString().padStart(4, "0")}`,
		path: `C:\\perf\\repo-${index.toString().padStart(4, "0")}`,
	}));
}
