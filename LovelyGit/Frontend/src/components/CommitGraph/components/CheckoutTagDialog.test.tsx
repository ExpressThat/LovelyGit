// @vitest-environment jsdom

import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { sendRequestWithResponse } from "@/lib/commands";
import { NativeMessageType } from "@/lib/nativeMessaging";
import { CheckoutTagDialog } from "./CheckoutTagDialog";

vi.mock("@/lib/commands", () => ({ sendRequestWithResponse: vi.fn() }));

describe("CheckoutTagDialog", () => {
	beforeEach(() => vi.clearAllMocks());

	it("checks out the tag and refreshes only after success", async () => {
		const user = userEvent.setup();
		const onClose = vi.fn();
		const onRepositoryChanged = vi.fn();
		vi.mocked(sendRequestWithResponse).mockResolvedValue(undefined);
		render(
			<CheckoutTagDialog
				onClose={onClose}
				onRepositoryChanged={onRepositoryChanged}
				repositoryId="repo"
				tagName="v2.0.0"
			/>,
		);

		await user.click(screen.getByRole("button", { name: "Checkout detached" }));

		await waitFor(() => expect(onClose).toHaveBeenCalledOnce());
		expect(sendRequestWithResponse).toHaveBeenCalledWith(
			{
				arguments: { repositoryId: "repo", tagName: "v2.0.0" },
				commandType: NativeMessageType.CheckoutTag,
			},
			expect.any(Object),
		);
		expect(onRepositoryChanged).toHaveBeenCalledOnce();
	});

	it("keeps the confirmation open after failure so it can retry", async () => {
		const user = userEvent.setup();
		const onClose = vi.fn();
		const onRepositoryChanged = vi.fn();
		vi.mocked(sendRequestWithResponse)
			.mockRejectedValueOnce(new Error("Local changes would be overwritten"))
			.mockResolvedValueOnce(undefined);
		render(
			<CheckoutTagDialog
				onClose={onClose}
				onRepositoryChanged={onRepositoryChanged}
				repositoryId="repo"
				tagName="v2.0.0"
			/>,
		);

		await user.click(screen.getByRole("button", { name: "Checkout detached" }));
		await waitFor(() =>
			expect(sendRequestWithResponse).toHaveBeenCalledTimes(1),
		);
		expect(onClose).not.toHaveBeenCalled();
		expect(onRepositoryChanged).not.toHaveBeenCalled();

		await user.click(screen.getByRole("button", { name: "Checkout detached" }));
		await waitFor(() => expect(onClose).toHaveBeenCalledOnce());
		expect(sendRequestWithResponse).toHaveBeenCalledTimes(2);
	});
});
