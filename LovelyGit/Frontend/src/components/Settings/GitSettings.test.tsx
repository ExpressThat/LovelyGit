// @vitest-environment jsdom

import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { setSetting, useSetting } from "@/lib/settings/settingsStore";
import { GitSettings } from "./GitSettings";

vi.mock("@/lib/settings/settingsStore", () => ({
	setSetting: vi.fn(),
	useSetting: vi.fn(),
}));

describe("GitSettings", () => {
	beforeEach(() => {
		vi.clearAllMocks();
		vi.mocked(useSetting).mockReturnValue(false);
	});

	it("persists the commit-signing default", async () => {
		const user = userEvent.setup();
		render(<GitSettings />);

		await user.click(
			screen.getByRole("switch", { name: /^Sign commits by default/ }),
		);

		expect(setSetting).toHaveBeenCalledWith("SignCommitsByDefault", true);
		expect(
			screen.getByText(
				"Uses Git's user.signingKey and gpg.format configuration.",
			),
		).toBeVisible();
	});
});
