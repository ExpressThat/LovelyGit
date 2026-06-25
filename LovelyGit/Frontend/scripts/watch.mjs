import { spawn } from "node:child_process";
import { readdir } from "node:fs/promises";
import { watch } from "node:fs";
import path from "node:path";

const frontendRoot = path.resolve(import.meta.dirname, "..");
const csharpRoot = path.resolve(frontendRoot, "..");
const ignoredDirectories = new Set([
	".git",
	".vs",
	"bin",
	"dist",
	"Frontend",
	"node_modules",
	"obj",
	"wwwroot",
]);

let shuttingDown = false;
let generationProcess = null;
let generationTimeout = null;
let pendingGenerationFile = null;
let watcherSyncTimeout = null;

const vite = spawn("pnpm", ["exec", "vite", "build", "--watch"], {
	cwd: frontendRoot,
	shell: process.platform === "win32",
	stdio: "inherit",
});

vite.on("exit", (code, signal) => {
	if (shuttingDown) {
		return;
	}

	console.error(
		`[watch] vite exited${signal ? ` with signal ${signal}` : ` with code ${code}`}.`,
	);
	shutdown(code ?? 1);
});

const watchers = new Map();
await syncWatchedDirectories();

process.on("SIGINT", () => shutdown(0));
process.on("SIGTERM", () => shutdown(0));

async function collectSourceDirectories(root) {
	const directories = [];

	async function visit(directory) {
		const entries = await readdir(directory, { withFileTypes: true });
		directories.push(directory);

		for (const entry of entries) {
			if (!entry.isDirectory() || ignoredDirectories.has(entry.name)) {
				continue;
			}

			await visit(path.join(directory, entry.name));
		}
	}

	await visit(root);
	return directories;
}

async function syncWatchedDirectories() {
	const directories = await collectSourceDirectories(csharpRoot);
	const nextDirectories = new Set(directories);

	for (const [directory, watcher] of watchers) {
		if (!nextDirectories.has(directory)) {
			watcher.close();
			watchers.delete(directory);
		}
	}

	for (const directory of directories) {
		if (watchers.has(directory)) {
			continue;
		}

		const watcher = watch(directory, (_eventType, filename) => {
			if (!filename) {
				return;
			}

			const changedPath = path.join(directory, filename.toString());
			handleSourceChange(changedPath);
		});

		watchers.set(directory, watcher);
	}

	console.log(`[watch] Watching ${watchers.size} C# source directories.`);
}

function handleSourceChange(changedPath) {
	const fileName = path.basename(changedPath);
	if (ignoredDirectories.has(fileName)) {
		return;
	}

	if (changedPath.endsWith(".cs")) {
		clearTimeout(generationTimeout);
		generationTimeout = setTimeout(() => {
			runTypeGeneration(changedPath);
		}, 200);
		return;
	}

	scheduleWatcherSync();
}

function scheduleWatcherSync() {
	clearTimeout(watcherSyncTimeout);
	watcherSyncTimeout = setTimeout(() => {
		syncWatchedDirectories().catch((error) => {
			console.error(
				error instanceof Error
					? error.message
					: "Failed to refresh C# source watchers.",
			);
		});
	}, 200);
}

function runTypeGeneration(changedFile) {
	if (generationProcess) {
		pendingGenerationFile = changedFile;
		return;
	}

	console.log(
		`[watch] C# change detected: ${path.relative(csharpRoot, changedFile)}`,
	);

	generationProcess = spawn("pnpm", ["generate:types"], {
		cwd: frontendRoot,
		shell: process.platform === "win32",
		stdio: "inherit",
	});

	generationProcess.on("exit", () => {
		generationProcess = null;

		if (pendingGenerationFile) {
			const pendingFile = pendingGenerationFile;
			pendingGenerationFile = null;
			runTypeGeneration(pendingFile);
		}
	});
}

function shutdown(code) {
	shuttingDown = true;

	for (const watcher of watchers.values()) {
		watcher.close();
	}

	if (generationProcess && !generationProcess.killed) {
		generationProcess.kill();
	}

	if (!vite.killed) {
		vite.kill();
	}

	process.exit(code);
}
