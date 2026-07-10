// @vitest-environment jsdom

import { act, renderHook } from "@testing-library/react";
import { useState } from "react";
import { describe, expect, it } from "vitest";
import type { DetailsPanelState } from "./AppPanelState";
import type { FileHistoryTarget } from "./components/FileHistory/FileHistoryDialog";
import { useResetOnRepositoryChange } from "./useResetOnRepositoryChange";

describe("useResetOnRepositoryChange", () => {
	it("clears repository-scoped overlays and selections when the tab changes", () => {
		const { result, rerender } = renderHook(
			({ repositoryId }) => {
				const [branch, setBranch] = useState<string | null>("main");
				const [details, setDetails] = useState<DetailsPanelState | null>({
					commitHash: "abc",
					kind: "commit",
				});
				const [history, setHistory] = useState<FileHistoryTarget | null>({
					path: "file.ts",
					startCommitHash: null,
				});
				const [searchOpen, setSearchOpen] = useState(true);
				useResetOnRepositoryChange(
					repositoryId,
					setBranch,
					setDetails,
					setHistory,
					setSearchOpen,
				);
				return { branch, details, history, searchOpen };
			},
			{ initialProps: { repositoryId: "one" } },
		);

		act(() => rerender({ repositoryId: "two" }));

		expect(result.current).toEqual({
			branch: null,
			details: null,
			history: null,
			searchOpen: false,
		});
	});
});
