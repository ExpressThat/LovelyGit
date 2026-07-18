// @vitest-environment jsdom

import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { PatchApplyPreview } from "./PatchApplyPreview";

describe("PatchApplyPreview", () => {
	it("explains when the native preview reached its safety limit", () => {
		render(
			<PatchApplyPreview
				disabled={false}
				onReverseChange={() => undefined}
				onStageChangesChange={() => undefined}
				preview={{
					fileName: "large.patch",
					files: [{ additions: 1, deletions: 0, path: "first.txt" }],
					isTruncated: true,
					path: "large.patch",
					selected: true,
					totalAdditions: 1,
					totalDeletions: 0,
				}}
				reverse={false}
				stageChanges={false}
			/>,
		);

		expect(
			screen.getByText("Preview limited to the first 1 file."),
		).toBeInTheDocument();
	});
});
