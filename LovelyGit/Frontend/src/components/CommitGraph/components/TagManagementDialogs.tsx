import type { CommitGraphRow } from "@/generated/types";
import type { TagMutationController } from "../hooks/useTagMutations";
import {
	LazyCheckoutTagDialog,
	LazyCreateTagDialog,
	LazyDeleteTagDialog,
} from "./LazyGraphManagementDialogs";

export function TagManagementDialogs({
	controller,
	existingTagNames,
	onCreateOpenChange,
	onRepositoryChanged,
	remoteName,
	repositoryId,
	tagCommit,
}: {
	controller: TagMutationController;
	existingTagNames: string[];
	onCreateOpenChange: (commit: CommitGraphRow | null) => void;
	onRepositoryChanged: () => void;
	remoteName: string | null;
	repositoryId: string | null;
	tagCommit: CommitGraphRow | null;
}) {
	const { busyTag, checkoutTagName, deleteTag, deleteTagName } = controller;
	return (
		<>
			{checkoutTagName ? (
				<LazyCheckoutTagDialog
					onClose={() => controller.setCheckoutTagName(null)}
					onRepositoryChanged={onRepositoryChanged}
					repositoryId={repositoryId}
					tagName={checkoutTagName}
				/>
			) : null}
			{tagCommit ? (
				<LazyCreateTagDialog
					commit={tagCommit}
					existingTagNames={existingTagNames}
					key={tagCommit.commit.hash}
					onOpenChange={onCreateOpenChange}
					onRepositoryChanged={onRepositoryChanged}
					remoteName={remoteName}
					repositoryId={repositoryId}
				/>
			) : null}
			<LazyDeleteTagDialog
				isBusy={busyTag !== null}
				onConfirm={() => void deleteTag()}
				onOpenChange={controller.setDeleteTagName}
				tagName={deleteTagName}
			/>
		</>
	);
}
