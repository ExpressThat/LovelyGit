// @vitest-environment jsdom

import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { KnownGitRepository } from "@/generated/types";
import { Tabs } from "./Tabs";

const state = vi.hoisted(() => ({
	closeRepository: vi.fn(),
	currentRepositoryId: null as string | null,
	repositories: [] as KnownGitRepository[],
	setCurrentRepositoryId: vi.fn(),
}));
vi.mock("@/lib/repositoryContext", () => ({
	useRepositoryContext: () => ({
		closeRepository: state.closeRepository,
		currentRepositoryId: state.currentRepositoryId,
		isLoadingRepositories: false,
		repositories: state.repositories,
		setCurrentRepositoryId: state.setCurrentRepositoryId,
	}),
}));

describe("Tabs", () => {
	beforeEach(() => {
		state.currentRepositoryId = null;
		state.repositories = [];
		vi.clearAllMocks();
	});

	it("keeps a large repository collection bounded", () => {
		state.repositories = repositories(100);
		state.currentRepositoryId = "0";
		const view = render(<Tabs />);
		const tabs = view.container.querySelectorAll("[data-repository-tab]");
		expect(
			view.container.querySelector("[data-repository-tabs='virtual']"),
		).toBeInTheDocument();
		expect(tabs.length).toBeLessThanOrEqual(8);
	});

	it("retains the animated layout for normal repository counts", () => {
		state.repositories = repositories(6);
		state.currentRepositoryId = "0";
		const view = render(<Tabs />);

		expect(
			view.container.querySelector("[data-repository-tabs='virtual']"),
		).not.toBeInTheDocument();
		expect(
			view.container.querySelectorAll("[data-repository-tab]"),
		).toHaveLength(6);
	});

	it("keeps overflowing ordinary tabs inside a compact scroll surface", () => {
		state.repositories = repositories(20);
		const view = render(<Tabs />);

		expect(view.container.querySelector(".tab-scrollbar")).toHaveClass(
			"overflow-x-auto",
			"overflow-y-hidden",
		);
		expect(screen.getByTitle("C:\\perf\\repo-0019")).toBeVisible();
	});

	it("brings an externally selected distant repository into the virtual window", async () => {
		state.repositories = repositories(100);
		state.currentRepositoryId = "99";
		render(<Tabs />);

		await waitFor(() =>
			expect(screen.getByTitle("C:\\perf\\repo-0099")).toBeInTheDocument(),
		);
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
