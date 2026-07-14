// @vitest-environment jsdom

import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { WorkingChangesHeader } from "./WorkingChangesPanelParts";

describe("WorkingChangesHeader", () => {
	it("disables refresh while a working-tree scan is active", () => {
		render(
			<WorkingChangesHeader isLoading onRefresh={vi.fn()} totalCount={1_800} />,
		);

		expect(
			screen.getByRole("button", { name: "Refresh working changes" }),
		).toBeDisabled();
	});
});
