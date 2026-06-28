import type { CommitDetailsResponse } from "@/generated/types";
import { shortHash } from "../CommitGraph/utils/format";

export type CommitDetailsCopyAction = {
	copyLabel: string;
	key: "hash" | "shortHash" | "subject" | "message";
	label: string;
	shortLabel: string;
	title: string;
	value: string;
};

export function buildCommitDetailsCopyActions(
	details: CommitDetailsResponse,
): CommitDetailsCopyAction[] {
	const actions: CommitDetailsCopyAction[] = [
		{
			copyLabel: "Commit hash",
			key: "hash",
			label: "Copy full hash",
			shortLabel: "Hash",
			title: "Copy full commit hash",
			value: details.hash,
		},
		{
			copyLabel: "Short hash",
			key: "shortHash",
			label: "Copy short hash",
			shortLabel: "Short",
			title: "Copy short commit hash",
			value: shortHash(details.hash),
		},
	];

	const subject = details.subject.trim();
	if (subject.length > 0) {
		actions.push({
			copyLabel: "Subject",
			key: "subject",
			label: "Copy subject",
			shortLabel: "Subject",
			title: "Copy commit subject",
			value: subject,
		});
	}

	const message = details.message.trim();
	if (message.length > 0 && message !== subject) {
		actions.push({
			copyLabel: "Message",
			key: "message",
			label: "Copy message",
			shortLabel: "Message",
			title: "Copy full commit message",
			value: details.message,
		});
	}

	return actions;
}
