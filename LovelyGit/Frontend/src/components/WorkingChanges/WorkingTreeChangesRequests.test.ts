import { beforeEach, describe, expect, it, vi } from "vitest";
import { sendRequestWithResponse } from "@/lib/commands";
import { loadWorkingTreeChanges } from "./WorkingTreeChangesRequests";

vi.mock("@/lib/commands", () => ({ sendRequestWithResponse: vi.fn() }));

const send = vi.mocked(sendRequestWithResponse);

describe("loadWorkingTreeChanges", () => {
	beforeEach(() => send.mockReset());

	it("shares concurrent full status requests", async () => {
		let complete: (value: ReturnTypeResponse) => void = () => undefined;
		send.mockReturnValueOnce(new Promise((resolve) => (complete = resolve)));

		const first = loadWorkingTreeChanges("repo");
		const second = loadWorkingTreeChanges("repo");
		expect(send).toHaveBeenCalledTimes(1);
		complete(response());

		await expect(first).resolves.toEqual(response());
		await expect(second).resolves.toEqual(response());
	});

	it("clears a failed in-flight request for retry", async () => {
		send
			.mockRejectedValueOnce(new Error("failed"))
			.mockResolvedValueOnce(response());

		await expect(loadWorkingTreeChanges("repo")).rejects.toThrow("failed");
		await expect(loadWorkingTreeChanges("repo")).resolves.toEqual(response());
		expect(send).toHaveBeenCalledTimes(2);
	});

	it("publishes tracked changes before completing untracked discovery", async () => {
		const preliminary = response(false);
		const complete = response();
		const onPreliminary = vi.fn();
		send.mockResolvedValueOnce(preliminary).mockResolvedValueOnce(complete);

		await expect(
			loadWorkingTreeChanges("repo", onPreliminary),
		).resolves.toEqual(complete);

		expect(onPreliminary).toHaveBeenCalledWith(preliminary);
		expect(send).toHaveBeenNthCalledWith(
			1,
			expect.objectContaining({
				arguments: expect.objectContaining({ trackedOnly: true }),
			}),
			expect.anything(),
		);
		expect(send).toHaveBeenNthCalledWith(
			2,
			expect.objectContaining({
				arguments: expect.objectContaining({ trackedOnly: false }),
			}),
			expect.anything(),
		);
	});

	it("does not publish or cache a complete result when the second phase fails", async () => {
		const onPreliminary = vi.fn();
		send
			.mockResolvedValueOnce(response(false))
			.mockRejectedValueOnce(new Error("full scan failed"));

		await expect(loadWorkingTreeChanges("repo", onPreliminary)).rejects.toThrow(
			"full scan failed",
		);
		expect(onPreliminary).toHaveBeenCalledWith(response(false));
	});

	it("falls back to the complete scan when the preliminary scan fails", async () => {
		const onPreliminary = vi.fn();
		send
			.mockRejectedValueOnce(new Error("preliminary failed"))
			.mockResolvedValueOnce(response());

		await expect(
			loadWorkingTreeChanges("repo", onPreliminary),
		).resolves.toEqual(response());
		expect(onPreliminary).not.toHaveBeenCalled();
	});
});

function response(isComplete = true) {
	return {
		isComplete,
		staged: [],
		unstaged: [],
		untracked: [],
		unmerged: [],
		totalCount: 0,
	};
}

type ReturnTypeResponse = ReturnType<typeof response>;
