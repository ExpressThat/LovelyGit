/// <reference types="node" />

import { globSync, readFileSync } from "node:fs";
import path from "node:path";
import { describe, expect, it } from "vitest";

const sourceRoot = path.resolve(import.meta.dirname, "..");
const allowedDirectImports = new Set([
	"lib/motion.ts",
	"lib/motionFeatures.ts",
	"test/motion.ts",
]);

describe("Motion import boundary", () => {
	it("keeps feature components on the deferred Motion facade", () => {
		const directImports = globSync("**/*.{ts,tsx}", { cwd: sourceRoot })
			.map((file) => file.replaceAll("\\", "/"))
			.filter((file) => !allowedDirectImports.has(file))
			.filter((file) =>
				/from ["']motion\/react(?:-m)?["']/.test(
					readFileSync(path.join(sourceRoot, file), "utf8"),
				),
			);

		expect(directImports).toEqual([]);
	});
});
