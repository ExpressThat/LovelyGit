import { HardDriveDownload } from "lucide-react";
import { lazy, Suspense, useState } from "react";
import { SurfaceLoading } from "@/AppLazySurfaces";
import { Dialog, DialogTrigger } from "@/components/ui/dialog";

const LfsManagerContent = lazy(() =>
	import("./LfsManagerContent").then((module) => ({
		default: module.LfsManagerContent,
	})),
);

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
				<Suspense fallback={<SurfaceLoading label="Opening Git LFS" overlay />}>
					<LfsManagerContent repositoryId={repositoryId} />
				</Suspense>
			) : null}
		</Dialog>
	);
}
