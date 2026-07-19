import { describe, expect, it } from "vitest";
import { encodeGzipBase64 } from "@/components/CommitFileDiff/compactPayloadCompression";
import type { GetRemotesResponse, GitRemote } from "@/generated/types";
import { expandRemotePayload, maximumRemoteCount } from "./remotePayload";

describe("expandRemotePayload", () => {
	it("restores every compact remote", async () => {
		const remotes: GitRemote[] = [
			{
				name: "origin",
				pushUrl: null,
				url: "https://example.invalid/origin.git",
			},
			{
				name: "backup",
				pushUrl: "ssh://example.invalid/backup.git",
				url: "https://example.invalid/backup.git",
			},
		];

		await expect(expandRemotePayload(await compact(remotes))).resolves.toEqual(
			remotes,
		);
	});

	it("preserves an ordinary response list by identity", async () => {
		const response = ordinary();

		await expect(expandRemotePayload(response)).resolves.toBe(response.remotes);
	});

	it("rejects malformed and oversized compact payloads", async () => {
		const malformed = await compressed([{ name: "origin", url: 42 }]);
		const oversized = await compact(
			Array.from({ length: maximumRemoteCount + 1 }, (_, index) => ({
				name: `remote-${index}`,
				pushUrl: null,
				url: "https://example.invalid/repository.git",
			})),
		);

		await expect(expandRemotePayload(malformed)).rejects.toThrow(
			"remote list is invalid",
		);
		await expect(expandRemotePayload(oversized)).rejects.toThrow(
			"remote list is invalid",
		);
	});
});

async function compact(remotes: GitRemote[]): Promise<GetRemotesResponse> {
	return compressed(remotes);
}

async function compressed(value: unknown): Promise<GetRemotesResponse> {
	return {
		compactRemotesGzipBase64: await encodeGzipBase64(JSON.stringify(value)),
		remotes: [],
	};
}

function ordinary(): GetRemotesResponse {
	return {
		compactRemotesGzipBase64: null,
		remotes: [
			{
				name: "origin",
				pushUrl: null,
				url: "https://example.invalid/origin.git",
			},
		],
	};
}
