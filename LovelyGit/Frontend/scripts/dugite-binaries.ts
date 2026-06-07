import { createHash } from "node:crypto";
import {
	createWriteStream,
	existsSync,
	mkdirSync,
	readFileSync,
	rmSync,
	statSync,
	unlinkSync,
	writeFileSync,
} from "node:fs";
import { readdir } from "node:fs/promises";
import { dirname, resolve } from "node:path";
import { pipeline } from "node:stream/promises";
import { fileURLToPath } from "node:url";

export type DugiteRuntime = {
	rid: string;
	platform: "ubuntu" | "windows" | "macOS";
	arch: "x64" | "arm64";
};

export type DugiteArtifact = DugiteRuntime & {
	artifact: string;
	sha256: string;
};

const __dirname = dirname(fileURLToPath(import.meta.url));
export const frontendRoot = resolve(__dirname, "..");
export const projectRoot = resolve(frontendRoot, "..");
export const repoRoot = resolve(projectRoot, "..");
export const releaseWorkflowPath = resolve(
	repoRoot,
	".github",
	"workflows",
	"release.yml",
);
export const dugiteManifestPath = resolve(
	projectRoot,
	"BundledTools",
	"Git",
	"dugite-native.json",
);
export const thirdPartyNoticePath = resolve(
	projectRoot,
	"ThirdPartyNotices",
	"Git",
	"NOTICE.md",
);
export const sourceOfferPath = resolve(
	projectRoot,
	"ThirdPartyNotices",
	"Git",
	"SOURCE-OFFER.md",
);
export const bundledGitPath = resolve(projectRoot, "BundledTools", "Git");
export const downloadsPath = resolve(repoRoot, "artifacts", "downloads");

export type DugiteManifest = {
	releaseTag: string;
	artifacts: Record<string, { artifact: string; sha256: string }>;
};

export const supportedRuntimes: readonly DugiteRuntime[] = [
	{ rid: "linux-x64", platform: "ubuntu", arch: "x64" },
	{ rid: "linux-arm64", platform: "ubuntu", arch: "arm64" },
	{ rid: "win-x64", platform: "windows", arch: "x64" },
	{ rid: "win-arm64", platform: "windows", arch: "arm64" },
	{ rid: "osx-x64", platform: "macOS", arch: "x64" },
	{ rid: "osx-arm64", platform: "macOS", arch: "arm64" },
];

export function normalizeReleaseTag(value: string): string {
	const version = value.trim();
	if (!version) {
		throw new Error("A dugite-native release version is required.");
	}

	return version.startsWith("v") ? version : `v${version}`;
}

export function getDugiteBaseUrl(releaseTag: string): string {
	return `https://github.com/desktop/dugite-native/releases/download/${releaseTag}`;
}

export function readDugiteManifest(): {
	releaseTag: string;
	artifacts: DugiteArtifact[];
} {
	const manifest = JSON.parse(
		readFileSync(dugiteManifestPath, "utf8"),
	) as DugiteManifest;

	const artifacts = supportedRuntimes.map((runtime) => {
		const entry = manifest.artifacts[runtime.rid];
		if (!entry) {
			throw new Error(
				`Could not find artifact config for ${runtime.rid} in manifest.`,
			);
		}

		return {
			...runtime,
			artifact: entry.artifact,
			sha256: entry.sha256.toLowerCase(),
		};
	});

	return {
		releaseTag: manifest.releaseTag,
		artifacts,
	};
}

export function writeDugiteManifest(
	releaseTag: string,
	artifacts: DugiteArtifact[],
): void {
	const manifest: DugiteManifest = {
		releaseTag,
		artifacts: Object.fromEntries(
			artifacts.map((artifact) => [
				artifact.rid,
				{
					artifact: artifact.artifact,
					sha256: artifact.sha256.toLowerCase(),
				},
			]),
		),
	};

	writeFileSync(
		`${dugiteManifestPath}`,
		`${JSON.stringify(manifest, null, 2)}\n`,
	);
}

export async function fetchReleaseAssetNames(
	releaseTag: string,
): Promise<string[]> {
	const response = await fetch(
		`https://api.github.com/repos/desktop/dugite-native/releases/tags/${releaseTag}`,
		{
			headers: {
				Accept: "application/vnd.github+json",
			},
		},
	);

	if (!response.ok) {
		throw new Error(
			`Failed to load dugite-native release ${releaseTag}: ${response.status} ${response.statusText}`,
		);
	}

	const release = (await response.json()) as {
		assets?: Array<{ name?: string }>;
	};

	return (release.assets ?? [])
		.map((asset) => asset.name)
		.filter((name): name is string => Boolean(name));
}

export function selectRuntimeArtifact(
	assetNames: readonly string[],
	runtime: DugiteRuntime,
): string {
	const artifact = assetNames.find(
		(name) =>
			name.endsWith(".tar.gz") &&
			name.includes(`-${runtime.platform}-${runtime.arch}.tar.gz`),
	);

	if (!artifact) {
		throw new Error(
			`No dugite-native asset found for ${runtime.rid} (${runtime.platform}-${runtime.arch}).`,
		);
	}

	return artifact;
}

export async function downloadFile(
	url: string,
	outputPath: string,
): Promise<void> {
	mkdirSync(dirname(outputPath), { recursive: true });

	const response = await fetch(url);
	if (!response.ok || !response.body) {
		throw new Error(
			`Failed to download ${url}: ${response.status} ${response.statusText}`,
		);
	}

	await pipeline(response.body, createWriteStream(outputPath));
}

export function sha256File(path: string): string {
	const hash = createHash("sha256");
	const file = readFileSync(path);
	hash.update(file);
	return hash.digest("hex");
}

export function detectLocalRid(): string {
	const platform = process.platform;
	const arch = process.arch;

	if (platform === "win32" && arch === "x64") {
		return "win-x64";
	}
	if (platform === "win32" && arch === "arm64") {
		return "win-arm64";
	}
	if (platform === "darwin" && arch === "x64") {
		return "osx-x64";
	}
	if (platform === "darwin" && arch === "arm64") {
		return "osx-arm64";
	}
	if (platform === "linux" && arch === "x64") {
		return "linux-x64";
	}
	if (platform === "linux" && arch === "arm64") {
		return "linux-arm64";
	}

	throw new Error(
		`Unsupported local platform/architecture: ${platform}/${arch}`,
	);
}

export async function clearBundledGitDirectory(): Promise<void> {
	mkdirSync(bundledGitPath, { recursive: true });

	for (const entry of await readdir(bundledGitPath)) {
		if (
			entry === ".gitignore" ||
			entry === "README.md" ||
			entry === "dugite-native.json"
		) {
			continue;
		}

		const entryPath = resolve(bundledGitPath, entry);
		const stats = statSync(entryPath);
		if (stats.isDirectory()) {
			rmSync(entryPath, { recursive: true, force: true });
		} else {
			unlinkSync(entryPath);
		}
	}
}

export function ensureGitLfsExists(root: string): void {
	const candidates = [
		resolve(root, "mingw64", "libexec", "git-core", "git-lfs.exe"),
		resolve(root, "libexec", "git-core", "git-lfs"),
		resolve(root, "bin", "git-lfs"),
	];

	if (!candidates.some((path) => existsSync(path))) {
		throw new Error(
			"Downloaded dugite-native payload does not include Git LFS.",
		);
	}
}

export function escapeRegExp(value: string): string {
	return value.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}
