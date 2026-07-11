// @vitest-environment jsdom

import { render, screen, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { SlidingDetailsPanel } from "./SlidingDetailsPanel";

vi.mock("@/lib/settings/settingsStore", () => ({
	setSetting: vi.fn(),
	useSetting: () => 440,
}));

describe("SlidingDetailsPanel", () => {
	it("opens at the persisted pixel width with an accessible splitter", async () => {
		render(
			<SlidingDetailsPanel isOpen onClose={vi.fn()} title="Commit details">
				<p>Details</p>
			</SlidingDetailsPanel>,
		);

		const splitter = screen.getByRole("button", {
			name: "Resize details panel",
		});
		const aside = splitter.closest("aside");
		expect(aside).not.toBeNull();
		await waitFor(() => expect(aside).toHaveStyle({ flexBasis: "440px" }));
		expect(screen.getByText("Details")).toBeInTheDocument();
	});
});
