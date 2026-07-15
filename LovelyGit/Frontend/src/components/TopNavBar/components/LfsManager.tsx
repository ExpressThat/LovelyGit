import { useState } from "react";
import { HardDriveDownload } from "@/components/icons/lovelyIcons";
import { Dialog, DialogTrigger } from "@/components/ui/dialog";

import { DeferredLfsManagerContent } from "./DeferredRepositoryManagerContent";

export function LfsManager({ repositoryId }: { repositoryId: string | null }) {
	const [open, setOpen] = useState(false);
	return (
		<Dialog onOpenChange={setOpen} open={open}>
			<DialogTrigger
				disabled={!repositoryId}
				render={
					<button
						aria-label="Manage Git LFS"
						className="inline-flex size-9 items-center justify-center rounded-md text-muted-foreground hover:bg-accent hover:text-accent-foreground disabled:pointer-events-none disabled:opacity-40"
						title="Git LFS"
						type="button"
					/>
				}
			>
				<HardDriveDownload aria-hidden="true" className="size-5" />
			</DialogTrigger>
			{open && repositoryId ? (
				<DeferredLfsManagerContent repositoryId={repositoryId} />
			) : null}
		</Dialog>
	);
}
