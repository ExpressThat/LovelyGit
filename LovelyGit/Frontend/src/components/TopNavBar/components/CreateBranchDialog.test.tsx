// @vitest-environment jsdom

import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { NativeMessageType } from "@/lib/nativeMessaging";
import { CreateBranchDialog } from "./CreateBranchDialog";

const mocks = vi.hoisted(() => ({ sendRequestWithResponse: vi.fn() }));

vi.mock("@/lib/commands", () => ({
	sendRequestWithResponse: mocks.sendRequestWithResponse,
}));
vi.mock("sonner", () => ({
	toast: { error: vi.fn(), loading: vi.fn(() => "toast-id"), success: vi.fn() },
}));

describe("CreateBranchDialog", () => {
	beforeEach(() => {
		mocks.sendRequestWithResponse.mockReset().mockResolvedValue(undefined);
	});

	it("creates and switches from an exact commit", async () => {
		const user = userEvent.setup();
		const onBranchChanged = vi.fn();
		renderDialog({ onBranchChanged });

		expect(
			screen.getByRole("heading", {
				name: "Create branch from commit 1111111",
			}),
		).toBeVisible();
		await user.type(
			screen.getByRole("textbox", { name: "Branch name" }),
			"feature/from-commit",
		);
		await user.click(screen.getByRole("button", { name: "Create and switch" }));

		expect(mocks.sendRequestWithResponse).toHaveBeenCalledWith(
			{
				arguments: {
					branchName: "feature/from-commit",
					checkout: true,
					repositoryId: "repository-id",
					startPoint: "1111111111111111111111111111111111111111",
				},
				commandType: NativeMessageType.CreateBranch,
			},
			expect.any(Object),
		);
		expect(onBranchChanged).toHaveBeenCalledWith("feature/from-commit");
	});

	it("can create without switching and rejects duplicate names", async () => {
		const user = userEvent.setup();
		const onBranchChanged = vi.fn();
		const onRepositoryChanged = vi.fn();
		renderDialog({ onBranchChanged, onRepositoryChanged });
		const input = screen.getByRole("textbox", { name: "Branch name" });

		await user.type(input, "main");
		expect(screen.getByText(/already exists/i)).toBeInTheDocument();
		expect(
			screen.getByRole("button", { name: "Create and switch" }),
		).toBeDisabled();
		await user.clear(input);
		await user.type(input, "release/v2");
		await user.click(
			screen.getByRole("switch", { name: /Switch to new branch/ }),
		);
		await user.click(screen.getByRole("button", { name: "Create branch" }));

		expect(mocks.sendRequestWithResponse).toHaveBeenCalledWith(
			expect.objectContaining({
				arguments: expect.objectContaining({ checkout: false }),
			}),
			expect.any(Object),
		);
		expect(onBranchChanged).not.toHaveBeenCalled();
		expect(onRepositoryChanged).toHaveBeenCalledOnce();
	});

	it("preserves the explicit source when the parent clears it", () => {
		const props = {
			currentBranchName: "main",
			onBranchChanged: vi.fn(),
			onOpenChange: vi.fn(),
			onRepositoryChanged: vi.fn(),
			repositoryId: "repository-id",
			source: {
				description: "Target commit",
				label: "commit 1111111",
				startPoint: "1111111111111111111111111111111111111111",
			},
		};
		const view = render(<CreateBranchDialog {...props} open />);

		view.rerender(<CreateBranchDialog {...props} open source={undefined} />);

		expect(
			screen.getByRole("heading", {
				name: "Create branch from commit 1111111",
			}),
		).toBeInTheDocument();
	});
});

function renderDialog({
	onBranchChanged,
	onRepositoryChanged = vi.fn(),
}: {
	onBranchChanged: (branchName: string) => void;
	onRepositoryChanged?: () => void;
}) {
	return render(
		<CreateBranchDialog
			currentBranchName="main"
			existingBranchNames={["main"]}
			onBranchChanged={onBranchChanged}
			onOpenChange={vi.fn()}
			onRepositoryChanged={onRepositoryChanged}
			open
			repositoryId="repository-id"
			source={{
				description: "Target commit",
				label: "commit 1111111",
				startPoint: "1111111111111111111111111111111111111111",
			}}
		/>,
	);
}
