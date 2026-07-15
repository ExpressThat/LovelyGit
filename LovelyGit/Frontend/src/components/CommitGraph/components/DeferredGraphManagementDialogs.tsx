import type { ComponentProps, ComponentType } from "react";
import { SurfaceLoading } from "@/AppLazySurfaces";
import { DeferredPrimaryOverlay } from "@/AppPrimaryOverlays";
import { createDeferredLoader } from "@/lib/deferredLoader";
import type { BranchComparisonDialog } from "./BranchComparisonDialog";
import type { BranchUpstreamDialog } from "./BranchUpstreamDialog";
import type { DeleteBranchDialog } from "./DeleteBranchDialog";
import type { RenameBranchDialog } from "./RenameBranchDialog";

export const DeferredBranchComparisonDialog = createDialog(
	() => import("./BranchComparisonDialog"),
	"BranchComparisonDialog",
);
export const DeferredBranchUpstreamDialog = createDialog(
	() => import("./BranchUpstreamDialog"),
	"BranchUpstreamDialog",
);
export const DeferredDeleteBranchDialog = createDialog(
	() => import("./DeleteBranchDialog"),
	"DeleteBranchDialog",
);
export const DeferredRenameBranchDialog = createDialog(
	() => import("./RenameBranchDialog"),
	"RenameBranchDialog",
);

type DialogModuleMap = {
	BranchComparisonDialog: typeof BranchComparisonDialog;
	BranchUpstreamDialog: typeof BranchUpstreamDialog;
	DeleteBranchDialog: typeof DeleteBranchDialog;
	RenameBranchDialog: typeof RenameBranchDialog;
};

function createDialog<TName extends keyof DialogModuleMap>(
	load: () => Promise<Record<TName, DialogModuleMap[TName]>>,
	name: TName,
) {
	type Props = ComponentProps<DialogModuleMap[TName]>;
	const loader = createDeferredLoader(() =>
		load().then((module) => module[name] as ComponentType<Props>),
	);
	return function DeferredGraphDialog(props: Props) {
		return (
			<DeferredPrimaryOverlay
				fallback={<SurfaceLoading label="Opening Git operation" overlay />}
				loader={loader}
				props={props}
			/>
		);
	};
}
