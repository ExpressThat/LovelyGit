import { describe, expect, it } from "vitest";
import { encodeGzipBase64 } from "@/components/CommitFileDiff/compactPayloadCompression";
import type { RepositoryRefsResponse } from "@/generated/types";
import { expandRepositoryRefsPayload } from "./repositoryRefsPayload";

describe("expandRepositoryRefsPayload", () => {
	it("restores compact refs and clears the compressed copy", async () => {
		const refs = [
			{
				commitHash: "abc",
				kind: "Local" as const,
				name: "feature/compact",
				remoteUrl: null,
			},
		];
		const response: RepositoryRefsResponse = {
			branchUpstreams: [],
			compactRefsGzipBase64: await encodeGzipBase64(JSON.stringify(refs)),
			currentBranchName: "main",
			refs: [],
			remotePrefixes: [],
			stashes: [],
			worktrees: [],
		};

		await expect(expandRepositoryRefsPayload(response)).resolves.toEqual({
			...response,
			compactRefsGzipBase64: null,
			refs,
		});
	});

	it("preserves ordinary responses by identity", async () => {
		const response = { compactRefsGzipBase64: null } as RepositoryRefsResponse;
		await expect(expandRepositoryRefsPayload(response)).resolves.toBe(response);
	});
});
