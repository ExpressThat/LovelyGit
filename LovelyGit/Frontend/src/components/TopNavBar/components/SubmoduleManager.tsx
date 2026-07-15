import { useState } from "react";
import { Boxes } from "@/components/icons/lovelyIcons";
import { Dialog, DialogTrigger } from "@/components/ui/dialog";

import { DeferredSubmoduleManagerContent } from "./DeferredRepositoryManagerContent";

export function SubmoduleManager({
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
						aria-label="Manage submodules"
						className="inline-flex size-9 items-center justify-center rounded-md text-muted-foreground hover:bg-accent hover:text-accent-foreground disabled:pointer-events-none disabled:opacity-40"
						title="Submodules"
						type="button"
					/>
				}
			>
				<Boxes aria-hidden="true" className="size-5" />
			</DialogTrigger>
			{open && repositoryId ? (
				<DeferredSubmoduleManagerContent repositoryId={repositoryId} />
			) : null}
		</Dialog>
	);
}
