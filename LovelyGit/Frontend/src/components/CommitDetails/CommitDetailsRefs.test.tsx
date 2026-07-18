// @vitest-environment jsdom

import { fireEvent, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import type { RepositoryRefItem } from "@/generated/types";
import { CommitDetailsRefs } from "./CommitDetailsRefs";

describe("CommitDetailsRefs", () => {
	it("shows a small reference set directly with semantic icons", () => {
		render(
			<CommitDetailsRefs
				refs={[
					ref("Local", "main"),
					ref("Remote", "origin/main"),
					ref("Tag", "v1"),
				]}
			/>,
		);

		expect(screen.getByText("3")).toBeVisible();
		expect(screen.getByTitle("main")).toBeVisible();
		expect(screen.getByTitle("origin/main")).toBeVisible();
		expect(screen.getByTitle("v1")).toBeVisible();
		expect(screen.queryByRole("button")).not.toBeInTheDocument();
	});

	it("keeps ten thousand refs bounded until explicitly expanded", async () => {
		const user = userEvent.setup();
		render(<CommitDetailsRefs refs={manyRefs()} />);

		expect(screen.getAllByTitle(/perf\/ref-/)).toHaveLength(3);
		await user.click(screen.getByRole("button", { name: /\+9997 more/ }));
		const list = document.querySelector(
			"[data-commit-details-ref-list='virtual']",
		);
		expect(list).toBeInTheDocument();
		expect(list?.querySelectorAll("[title]").length).toBeLessThanOrEqual(10);
	});

	it("filters the virtual list and reports an empty result", async () => {
		const user = userEvent.setup();
		render(<CommitDetailsRefs refs={manyRefs()} />);
		await user.click(screen.getByRole("button", { name: /\+9997 more/ }));
		const filter = screen.getByPlaceholderText("Filter references");

		fireEvent.input(filter, { target: { value: "ref-09999" } });
		expect(screen.getByTitle("perf/ref-09999")).toBeInTheDocument();
		fireEvent.input(filter, { target: { value: "missing" } });
		expect(
			screen.getByText("No references match this filter."),
		).toBeInTheDocument();
	});
});

function manyRefs() {
	return Array.from({ length: 10_000 }, (_, index) =>
		ref("Local", `perf/ref-${index.toString().padStart(5, "0")}`),
	);
}

function ref(kind: RepositoryRefItem["kind"], name: string): RepositoryRefItem {
	return { commitHash: "target", kind, name, remoteUrl: null };
}
