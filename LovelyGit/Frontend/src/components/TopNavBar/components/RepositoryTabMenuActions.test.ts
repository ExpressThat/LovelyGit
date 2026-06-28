import { describe, expect, it } from "vitest";
import { getRepositoryTabMenuActions } from "./RepositoryTabMenuActions";

describe("getRepositoryTabMenuActions", () => {
	it("includes repository path actions when the tab has a path", () => {
		expect(
			getRepositoryTabMenuActions({ hasPath: true, isActive: false }),
		).toEqual(["copy-path", "reveal", "open-terminal", "select", "close"]);
	});

	it("hides path and select actions when they do not apply", () => {
		expect(
			getRepositoryTabMenuActions({ hasPath: false, isActive: true }),
		).toEqual(["close"]);
	});
});
