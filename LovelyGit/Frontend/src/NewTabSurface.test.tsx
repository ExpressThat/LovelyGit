// @vitest-environment jsdom

import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { NewTabSurface } from "./NewTabSurface";

vi.mock("./components/NewTab/NewTab", () => ({
	NewTab: () => <div>Repository onboarding loaded</div>,
}));

describe("NewTabSurface", () => {
	it("loads repository onboarding through its route boundary", async () => {
		render(<NewTabSurface />);
		expect(
			await screen.findByText("Repository onboarding loaded"),
		).toBeVisible();
	});
});
