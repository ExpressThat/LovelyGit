import type { CommitGraphRow } from "@/generated/types";
import { CreateTagDialog } from "./CreateTagDialog";
import { DeleteTagDialog } from "./DeleteTagDialog";

export function TagManagementDialogs({
	busyTag,
	existingTagNames,
	onCreateOpenChange,
	onDelete,
	onDeleteOpenChange,
	onRepositoryChanged,
	remoteName,
	repositoryId,
	tagCommit,
	deleteTagName,
}: {
	busyTag: string | null;
	deleteTagName: string | null;
	existingTagNames: string[];
	onCreateOpenChange: (commit: CommitGraphRow | null) => void;
	onDelete: () => void;
	onDeleteOpenChange: (tagName: string | null) => void;
	onRepositoryChanged: () => void;
	remoteName: string | null;
	repositoryId: string | null;
	tagCommit: CommitGraphRow | null;
}) {
	return (
		<>
			{tagCommit ? (
				<CreateTagDialog
					commit={tagCommit}
					existingTagNames={existingTagNames}
					key={tagCommit.commit.hash}
					onOpenChange={onCreateOpenChange}
					onRepositoryChanged={onRepositoryChanged}
					remoteName={remoteName}
					repositoryId={repositoryId}
				/>
			) : null}
			<DeleteTagDialog
				isBusy={busyTag !== null}
				onConfirm={onDelete}
				onOpenChange={onDeleteOpenChange}
				tagName={deleteTagName}
			/>
		</>
	);
}
