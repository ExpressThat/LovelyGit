import {
	GitCommitHorizontal,
	GitMerge,
	ListRestart,
	Undo2,
} from "@/components/icons/lovelyIcons";
import type { GitRepositoryOperationKind } from "@/generated/types";

export function repositoryOperationLabel(
	operation: GitRepositoryOperationKind,
) {
	if (operation === "CherryPick") return "Cherry-pick";
	if (operation === "Rebase") return "Rebase";
	if (operation === "Revert") return "Revert";
	return "Merge";
}

export function RepositoryOperationIcon({
	operation,
}: {
	operation: GitRepositoryOperationKind;
}) {
	const className = "mt-0.5 size-4 text-primary";
	if (operation === "CherryPick") {
		return <GitCommitHorizontal aria-hidden="true" className={className} />;
	}
	if (operation === "Rebase") {
		return <ListRestart aria-hidden="true" className={className} />;
	}
	if (operation === "Revert") {
		return <Undo2 aria-hidden="true" className={className} />;
	}
	return <GitMerge aria-hidden="true" className={className} />;
}
