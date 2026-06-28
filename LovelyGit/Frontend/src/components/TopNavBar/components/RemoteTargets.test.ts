import { describe, expect, it } from "vitest";
import type { GitRemote } from "@/generated/types";
import { preferredRemoteName } from "./RemoteTargets";

describe("preferredRemoteName", () => {
	it("prefers origin, then the first configured remote", () => {
		expect(preferredRemoteName([])).toBeNull();
		expect(preferredRemoteName([remote("upstream")])).toBe("upstream");
		expect(preferredRemoteName([remote("fork"), remote("origin")])).toBe(
			"origin",
		);
	});
});

function remote(name: string): GitRemote {
	return { name, url: `https://example.test/${name}.git` };
}
