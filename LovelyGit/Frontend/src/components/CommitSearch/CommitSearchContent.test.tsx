// @vitest-environment jsdom

import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { CommitSearchContent } from "./CommitSearchContent";
import { searchResponse, searchResult } from "./CommitSearchTestData";

describe("CommitSearchContent", () => {
	it("bounds maximum result rendering and preserves selection actions", () => {
		const results = Array.from({ length: 100 }, (_, index) =>
			searchResult({
				hash: index.toString(16).padStart(40, "0"),
				subject: `Search result ${index}`,
			}),
		);
		const onSelect = vi.fn();
		render(
			<CommitSearchContent
				canSearchDeeper={false}
				error={null}
				isLoading={false}
				minimumQueryLength={2}
				onSearchDeeper={vi.fn()}
				onSelect={onSelect}
				onSelectIndex={vi.fn()}
				query="result"
				reduceMotion
				response={searchResponse(results)}
				selectedIndex={0}
			/>,
		);

		expect(
			screen.getAllByRole("button", { name: /Search result/ }),
		).toHaveLength(10);
		expect(
			screen.queryByRole("button", { name: /Search result 99/ }),
		).toBeNull();
		fireEvent.click(screen.getByRole("button", { name: /Search result 0/ }));
		expect(onSelect).toHaveBeenCalledWith(0);
	});
});
