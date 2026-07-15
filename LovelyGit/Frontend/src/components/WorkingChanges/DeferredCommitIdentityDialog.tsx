import type { ComponentProps } from "react";
import { SurfaceLoading } from "@/AppLazySurfaces";
import { DeferredPrimaryOverlay } from "@/AppPrimaryOverlays";
import { createDeferredLoader } from "@/lib/deferredLoader";
import type { CommitIdentityDialog } from "./CommitIdentityDialog";

type Props = ComponentProps<typeof CommitIdentityDialog>;

const loader = createDeferredLoader(() =>
	import("./CommitIdentityDialog").then(
		(module) => module.CommitIdentityDialog,
	),
);

export function DeferredCommitIdentityDialog(props: Props) {
	return (
		<DeferredPrimaryOverlay
			fallback={<SurfaceLoading label="Opening commit identity" overlay />}
			loader={loader}
			props={props}
		/>
	);
}
