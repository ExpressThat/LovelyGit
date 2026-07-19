import { describe, expect, it } from "vitest";
import { encodeGzipBase64 } from "@/components/CommitFileDiff/compactPayloadCompression";
import type { KnownGitRepository } from "@/generated/types";
import { expandKnownRepositories } from "./knownRepositoriesPayload";

const repositories: KnownGitRepository[] = [
	{
		id: "7534f22a-753a-4e7a-a9f2-77b9fa1c475f",
		name: "LovelyGit",
		path: "C:\\Projects\\LovelyGit",
	},
];

describe("expandKnownRepositories", () => {
	it("returns the direct small-list payload", async () => {
		await expect(
			expandKnownRepositories({
				compactRepositoriesGzipBase64: null,
				repositories,
			}),
		).resolves.toBe(repositories);
	});

	it("expands a compact large-list payload", async () => {
		const compactRepositoriesGzipBase64 = await encodeGzipBase64(
			JSON.stringify(repositories),
		);

		await expect(
			expandKnownRepositories({
				compactRepositoriesGzipBase64,
				repositories: [],
			}),
		).resolves.toEqual(repositories);
	});

	it("rejects malformed compact data without substituting a partial list", async () => {
		await expect(
			expandKnownRepositories({
				compactRepositoriesGzipBase64: "not-base64",
				repositories: [],
			}),
		).rejects.toThrow();
	});
});
