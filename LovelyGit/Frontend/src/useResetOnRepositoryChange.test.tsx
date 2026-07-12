// @vitest-environment jsdom

import { act, renderHook } from "@testing-library/react";
import { useState } from "react";
import { describe, expect, it } from "vitest";
import type { DetailsPanelState } from "./AppPanelState";
import { useResetOnRepositoryChange } from "./useResetOnRepositoryChange";

describe("useResetOnRepositoryChange", () => {
	it("clears repository-scoped overlays and selections when the tab changes", () => {
		const scopeHistory: Array<{ repositoryId: string; isCurrent: boolean }> =
			[];
		const { result, rerender } = renderHook(
			({ repositoryId }) => {
				const [branch, setBranch] = useState<string | null>("main");
				const [details, setDetails] = useState<DetailsPanelState | null>({
					commitHash: "abc",
					kind: "commit",
				});
				const [fileDiscoveryOpen, setFileDiscoveryOpen] = useState(true);
				const [searchOpen, setSearchOpen] = useState(true);
				const isCurrent = useResetOnRepositoryChange(
					repositoryId,
					setBranch,
					setDetails,
					() => setSearchOpen(false),
					() => setFileDiscoveryOpen(false),
				);
				scopeHistory.push({ repositoryId, isCurrent });
				return { branch, details, fileDiscoveryOpen, searchOpen, isCurrent };
			},
			{ initialProps: { repositoryId: "one" } },
		);

		act(() => rerender({ repositoryId: "two" }));

		expect(result.current).toEqual({
			branch: null,
			details: null,
			fileDiscoveryOpen: false,
			isCurrent: true,
			searchOpen: false,
		});
		expect(scopeHistory).toContainEqual({
			repositoryId: "two",
			isCurrent: false,
		});
	});
});
