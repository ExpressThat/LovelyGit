import {
	existsSync,
	mkdirSync,
	rmSync,
	statSync,
	unlinkSync,
} from "node:fs";
import { readdir } from "node:fs/promises";
import { resolve } from "node:path";

export async function clearDirectory(root: string): Promise<void> {
	mkdirSync(root, { recursive: true });
	for (const entry of await readdir(root)) {
		if (entry === ".gitignore" || entry === "README.md" || entry === "dugite-native.json") continue;
		const entryPath = resolve(root, entry);
		if (statSync(entryPath).isDirectory()) {
			rmSync(entryPath, { recursive: true, force: true });
		} else {
			unlinkSync(entryPath);
		}
	}
}

export function assertGitLfsExists(root: string): void {
	const candidates = [
		resolve(root, "mingw64", "libexec", "git-core", "git-lfs.exe"),
		resolve(root, "libexec", "git-core", "git-lfs"),
		resolve(root, "bin", "git-lfs"),
	];
	if (!candidates.some((path) => existsSync(path))) {
		throw new Error("Downloaded dugite-native payload does not include Git LFS.");
	}
}
