import { useState } from "react";
import { FolderGit2 } from "@/components/icons/lovelyIcons";
import { Dialog, DialogTrigger } from "@/components/ui/dialog";

import { DeferredSparseCheckoutManagerContent } from "./DeferredRepositoryManagerContent";

export function SparseCheckoutManager({
	repositoryId,
}: {
	repositoryId: string | null;
}) {
	const [open, setOpen] = useState(false);
	return (
		<Dialog onOpenChange={setOpen} open={open}>
			<DialogTrigger
				disabled={!repositoryId}
				render={
					<button
						aria-label="Manage sparse checkout"
						className="inline-flex size-9 items-center justify-center rounded-md text-muted-foreground hover:bg-accent hover:text-accent-foreground disabled:pointer-events-none disabled:opacity-40"
						title="Sparse checkout"
						type="button"
					/>
				}
			>
				<FolderGit2 aria-hidden="true" className="size-5" />
			</DialogTrigger>
			{open && repositoryId ? (
				<DeferredSparseCheckoutManagerContent repositoryId={repositoryId} />
			) : null}
		</Dialog>
	);
}
