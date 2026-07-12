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
});

function response() {
	return {
		staged: [],
		unstaged: [],
		untracked: [],
		unmerged: [],
		totalCount: 0,
	};
}

type ReturnTypeResponse = ReturnType<typeof response>;
