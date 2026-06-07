import { spawn } from "node:child_process";
import { existsSync } from "node:fs";
import { resolve } from "node:path";
import {
	bundledGitPath,
	clearBundledGitDirectory,
	detectLocalRid,
	downloadFile,
	downloadsPath,
	ensureGitLfsExists,
	getDugiteBaseUrl,
	readDugiteManifest,
	sha256File,
} from "./dugite-binaries.ts";

const requestedRid = process.argv[2];
const rid = requestedRid || detectLocalRid();
const config = readDugiteManifest();
const entry = config.artifacts.find((artifact) => artifact.rid === rid);

if (!entry) {
	throw new Error(`No dugite-native artifact configured for ${rid}.`);
}

const archivePath = resolve(downloadsPath, entry.artifact);
const archiveUrl = `${getDugiteBaseUrl(config.releaseTag)}/${entry.artifact}`;

if (!existsSync(archivePath) || sha256File(archivePath) !== entry.sha256) {
	console.log(`Downloading ${entry.artifact}`);
	await downloadFile(archiveUrl, archivePath);
}

const actualSha = sha256File(archivePath);
if (actualSha !== entry.sha256) {
	throw new Error(
		`SHA-256 mismatch for ${entry.artifact}\nExpected: ${entry.sha256}\nActual:   ${actualSha}`,
	);
}

await clearBundledGitDirectory();
await extractTarGz(archivePath, bundledGitPath);
ensureGitLfsExists(bundledGitPath);

console.log(
	`Downloaded ${config.releaseTag} ${rid} Git binaries to ${bundledGitPath}`,
);

async function extractTarGz(
	archivePath: string,
	outputDirectory: string,
): Promise<void> {
	await new Promise<void>((resolvePromise, reject) => {
		const tar = spawn("tar", ["-xzf", archivePath, "-C", outputDirectory], {
			stdio: "inherit",
		});

		tar.on("error", reject);
		tar.on("exit", (code) => {
			if (code === 0) {
				resolvePromise();
			} else {
				reject(new Error(`tar exited with code ${code ?? "unknown"}.`));
			}
		});
	});
}
