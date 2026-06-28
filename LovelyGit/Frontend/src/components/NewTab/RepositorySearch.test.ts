import { describe, expect, it } from "vitest";
import type { KnownGitRepository } from "@/generated/types";
import { filterRepositories } from "./RepositorySearch";

describe("filterRepositories", () => {
	it("matches repository names and paths with multiple terms", () => {
		const repositories = [
			repository("1", "LovelyGit", "C:\\Projects\\LovelyGit"),
			repository("2", "Chromium", "C:\\Projects\\chromium-tessting"),
			repository("3", "Docs", "/home/ross/docs"),
		];

		expect(filterRepositories(repositories, "lovely")).toEqual([
			repositories[0],
		]);
		expect(filterRepositories(repositories, "projects chromium")).toEqual([
			repositories[1],
		]);
		expect(filterRepositories(repositories, "ross/docs")).toEqual([
			repositories[2],
		]);
		expect(filterRepositories(repositories, "missing")).toEqual([]);
	});
});

function repository(
	id: string,
	name: string,
	path: string,
): KnownGitRepository {
	return { id, name, path };
}
