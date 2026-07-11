import { lazy, Suspense, useState } from "react";
import { SurfaceLoading } from "@/AppLazySurfaces";
import { FolderGit2 } from "@/components/icons/lovelyIcons";
import { Dialog, DialogTrigger } from "@/components/ui/dialog";

const SparseCheckoutManagerContent = lazy(() =>
	import("./SparseCheckoutManagerContent").then((module) => ({
		default: module.SparseCheckoutManagerContent,
	})),
);

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
				<Suspense
					fallback={<SurfaceLoading label="Opening sparse checkout" overlay />}
				>
					<SparseCheckoutManagerContent repositoryId={repositoryId} />
				</Suspense>
			) : null}
		</Dialog>
	);
}
