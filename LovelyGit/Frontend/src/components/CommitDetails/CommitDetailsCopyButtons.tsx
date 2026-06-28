import { Copy } from "lucide-react";
import { Button } from "@/components/ui/button";
import type { CommitDetailsResponse } from "@/generated/types";
import { copyToClipboard } from "../CommitGraph/utils/clipboard";
import { buildCommitDetailsCopyActions } from "./CommitDetailsCopyActions";

export function CommitDetailsCopyButtons({
	details,
}: {
	details: CommitDetailsResponse;
}) {
	const actions = buildCommitDetailsCopyActions(details);

	return (
		<fieldset className="flex flex-wrap items-center gap-1">
			<legend className="sr-only">Commit copy actions</legend>
			{actions.map((action) => (
				<Button
					aria-label={action.label}
					key={action.key}
					onClick={() => void copyToClipboard(action.value, action.copyLabel)}
					size="xs"
					title={action.title}
					type="button"
					variant="ghost"
				>
					<Copy aria-hidden="true" />
					<span>{action.shortLabel}</span>
				</Button>
			))}
		</fieldset>
	);
}
