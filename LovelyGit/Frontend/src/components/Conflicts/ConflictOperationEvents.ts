import type { GitConflictStateResponse } from "@/generated/types";

const eventName = "lovelygit:git-operation-changed";

type GitOperationChangedDetail = {
	repositoryId: string;
	state?: GitConflictStateResponse;
};

export function notifyGitOperationChanged(
	repositoryId: string,
	state?: GitConflictStateResponse,
) {
	window.dispatchEvent(
		new CustomEvent<GitOperationChangedDetail>(eventName, {
			detail: { repositoryId, state },
		}),
	);
}

export function subscribeGitOperationChanged(
	listener: (detail: GitOperationChangedDetail) => void,
) {
	const eventListener = (event: Event) => {
		listener((event as CustomEvent<GitOperationChangedDetail>).detail);
	};
	window.addEventListener(eventName, eventListener);
	return () => window.removeEventListener(eventName, eventListener);
}
