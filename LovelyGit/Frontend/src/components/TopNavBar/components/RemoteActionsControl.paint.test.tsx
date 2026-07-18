// @vitest-environment jsdom

import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { expect, it, vi } from "vitest";
import { RemoteActionsControl } from "./RemoteActionsControl";

const send = vi.hoisted(() => vi.fn(async () => undefined));
const waitForPaint = vi.hoisted(() => vi.fn<() => Promise<void>>());

vi.mock("sonner", () => ({
	toast: {
		error: vi.fn(),
		loading: vi.fn(() => "toast-1"),
		success: vi.fn(),
	},
}));
vi.mock("@/lib/commands", () => ({ sendRequestWithResponse: send }));
vi.mock("@/lib/waitForBrowserPaint", () => ({
	waitForBrowserPaint: waitForPaint,
}));
vi.mock("@/lib/settings/settingsStore", () => ({
	setSetting: vi.fn(),
	useSetting: () => "Fetch",
}));
vi.mock("./useRemoteSyncStatus", () => ({
	useRemoteSyncStatus: () => ({ reload: vi.fn(), status: null }),
}));

it("paints disabled controls before entering the native bridge", async () => {
	const user = userEvent.setup();
	let finishPaint: (() => void) | undefined;
	waitForPaint.mockReturnValueOnce(
		new Promise<void>((resolve) => {
			finishPaint = resolve;
		}),
	);
	render(
		<RemoteActionsControl currentBranchName="main" repositoryId="repo-1" />,
	);

	await user.click(screen.getByRole("button", { name: "Fetch all" }));
	expect(screen.getByRole("button", { name: "Fetch all" })).toBeDisabled();
	expect(send).not.toHaveBeenCalled();

	finishPaint?.();
	await waitFor(() => expect(send).toHaveBeenCalledOnce());
});
