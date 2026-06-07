import { readFileSync, writeFileSync } from "node:fs";
import { resolve } from "node:path";
import {
	type DugiteArtifact,
	downloadFile,
	downloadsPath,
	fetchReleaseAssetNames,
	getDugiteBaseUrl,
	normalizeReleaseTag,
	selectRuntimeArtifact,
	sha256File,
	sourceOfferPath,
	supportedRuntimes,
	thirdPartyNoticePath,
	writeDugiteManifest,
} from "./dugite-binaries.ts";

const releaseTag = normalizeReleaseTag(process.argv[2] ?? "");
const assetNames = await fetchReleaseAssetNames(releaseTag);
const baseUrl = getDugiteBaseUrl(releaseTag);
const artifacts: DugiteArtifact[] = [];

for (const runtime of supportedRuntimes) {
	const artifact = selectRuntimeArtifact(assetNames, runtime);
	const downloadPath = resolve(downloadsPath, artifact);

	console.log(`Downloading ${artifact}`);
	await downloadFile(`${baseUrl}/${artifact}`, downloadPath);

	const sha256 = sha256File(downloadPath);
	artifacts.push({ ...runtime, artifact, sha256 });
	console.log(`${runtime.rid}: ${sha256}`);
}

writeDugiteManifest(releaseTag, artifacts);
updateNotice(releaseTag, artifacts);
updateSourceOffer(releaseTag);

console.log(`Updated dugite-native manifest to ${releaseTag}.`);

function updateNotice(releaseTag: string, artifacts: DugiteArtifact[]): void {
	let notice = readFileSync(thirdPartyNoticePath, "utf8");
	const artifactLines = artifacts
		.map((artifact) => `${artifact.rid.padEnd(12)} ${artifact.artifact}`)
		.join("\n");
	const sourceUrl = `https://github.com/desktop/dugite-native/tree/${releaseTag}`;
	const dependenciesUrl = `https://raw.githubusercontent.com/desktop/dugite-native/${releaseTag}/dependencies.json`;

	notice = notice.replace(/Release:\s+v[^\n]+/, `Release:  ${releaseTag}`);
	notice = notice.replace(
		/Commit:\s+[^\n]+/,
		`Commit:   ${readCommitFromArtifact(artifacts[0].artifact)}`,
	);
	notice = notice.replace(
		/The release artifacts used by LovelyGit are selected by runtime identifier:\r?\n\r?\n```text\r?\n[\s\S]*?\r?\n```/,
		`The release artifacts used by LovelyGit are selected by runtime identifier:\n\n\`\`\`text\n${artifactLines}\n\`\`\``,
	);
	notice = notice.replace(
		/https:\/\/github\.com\/desktop\/dugite-native\/tree\/v[^\s`]+/,
		sourceUrl,
	);
	notice = notice.replace(
		/https:\/\/raw\.githubusercontent\.com\/desktop\/dugite-native\/v[^\s`]+\/dependencies\.json/,
		dependenciesUrl,
	);

	writeFileSync(thirdPartyNoticePath, notice);
}

function updateSourceOffer(releaseTag: string): void {
	let sourceOffer = readFileSync(sourceOfferPath, "utf8");
	sourceOffer = sourceOffer.replace(
		/https:\/\/github\.com\/desktop\/dugite-native\/tree\/v[^\s`]+/,
		`https://github.com/desktop/dugite-native/tree/${releaseTag}`,
	);
	sourceOffer = sourceOffer.replace(
		/https:\/\/raw\.githubusercontent\.com\/desktop\/dugite-native\/v[^\s`]+\/dependencies\.json/,
		`https://raw.githubusercontent.com/desktop/dugite-native/${releaseTag}/dependencies.json`,
	);

	writeFileSync(sourceOfferPath, sourceOffer);
}

function readCommitFromArtifact(artifact: string): string {
	const match = artifact.match(/^dugite-native-v[^-]+-([^-]+)-/);
	if (!match) {
		throw new Error(
			`Could not read dugite commit from artifact name: ${artifact}`,
		);
	}

	return match[1];
}
