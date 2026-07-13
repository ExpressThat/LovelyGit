// @vitest-environment jsdom

import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import type { RepositoryRefItem } from "@/generated/types";
import {
	CommitSearchScopeField,
	findRefSuggestions,
} from "./CommitSearchScopeField";

describe("CommitSearchScopeField", () => {
	it("bounds and prioritizes suggestions without rendering the whole ref set", async () => {
		const user = userEvent.setup();
		const onChange = vi.fn();
		render(
			<CommitSearchScopeField
				isLoading={false}
				onChange={onChange}
				refs={Array.from({ length: 500 }, (_, index) =>
					ref(`feature/${index}`),
				)}
				value="feature/4"
			/>,
		);

		const input = screen.getByRole("combobox", { name: /branch or tag/i });
		await user.click(input);
		expect(screen.getAllByRole("option")).toHaveLength(8);
		await user.keyboard("{Enter}");
		expect(onChange).toHaveBeenCalledWith("feature/4");
	});

	it("excludes stashes and keeps tag and branch kinds distinguishable", () => {
		const suggestions = findRefSuggestions(
			[ref("main"), ref("v1.0", "Tag"), ref("stash@{0}", "Stash")],
			"",
		);

		expect(suggestions.map((item) => [item.name, item.kind])).toEqual([
			["main", "Local"],
			["v1.0", "Tag"],
		]);
	});
});

function ref(
	name: string,
	kind: RepositoryRefItem["kind"] = "Local",
): RepositoryRefItem {
	return { commitHash: "a".repeat(40), kind, name, remoteUrl: null };
}
