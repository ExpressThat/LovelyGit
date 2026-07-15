// @vitest-environment jsdom

import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { BisectControl } from "./BisectControl";
import { useBisectSession } from "./useBisectSession";

vi.mock("./useBisectSession", () => ({ useBisectSession: vi.fn() }));
vi.mock("./BisectSessionContent", () => ({
	BisectSessionContent: () => <div>Bisect session loaded</div>,
}));

const load = vi.fn().mockResolvedValue(undefined);
const run = vi.fn().mockResolvedValue(undefined);
const session = vi.mocked(useBisectSession);

describe("BisectControl", () => {
	beforeEach(() => {
		vi.clearAllMocks();
		session.mockReturnValue({
			busyAction: null,
			isLoading: false,
			load,
			run,
			state: null,
		});
	});

	it("reveals meaningful bisect content without a suspense throttle", async () => {
		const user = userEvent.setup();
		render(<BisectControl repositoryId="repo" />);

		await user.click(screen.getByRole("button", { name: "Manage bisect" }));

		expect(await screen.findByText("Bisect session loaded")).toBeVisible();
		expect(load).toHaveBeenCalledOnce();
	});

	it("stays disabled when no repository is selected", () => {
		render(<BisectControl repositoryId={null} />);

		expect(
			screen.getByRole("button", { name: "Manage bisect" }),
		).toBeDisabled();
		expect(load).not.toHaveBeenCalled();
	});
});
