import type { ComponentProps, ComponentType } from "react";
import { SurfaceLoading } from "@/AppLazySurfaces";
import { DeferredPrimaryOverlay } from "@/AppPrimaryOverlays";
import { createDeferredLoader } from "@/lib/deferredLoader";
import type { CheckoutCommitDialog } from "./CheckoutCommitDialog";
import type { CherryPickDialog } from "./CherryPickDialog";
import type { InteractiveRebaseDialog } from "./InteractiveRebaseDialog";
import type { ReflogDialog } from "./ReflogDialog";
import type { ReflogResetDialog } from "./ReflogResetDialog";
import type { ResetCommitDialog } from "./ResetCommitDialog";
import type { RevertDialog } from "./RevertDialog";

export const DeferredCherryPickDialog = createDialog(
	() => import("./CherryPickDialog"),
	"CherryPickDialog",
);
export const DeferredCheckoutCommitDialog = createDialog(
	() => import("./CheckoutCommitDialog"),
	"CheckoutCommitDialog",
);
export const DeferredInteractiveRebaseDialog = createDialog(
	() => import("./InteractiveRebaseDialog"),
	"InteractiveRebaseDialog",
);
export const DeferredReflogDialog = createDialog(
	() => import("./ReflogDialog"),
	"ReflogDialog",
);
export const DeferredReflogResetDialog = createDialog(
	() => import("./ReflogResetDialog"),
	"ReflogResetDialog",
);
export const DeferredResetCommitDialog = createDialog(
	() => import("./ResetCommitDialog"),
	"ResetCommitDialog",
);
export const DeferredRevertDialog = createDialog(
	() => import("./RevertDialog"),
	"RevertDialog",
);

type DialogModuleMap = {
	CheckoutCommitDialog: typeof CheckoutCommitDialog;
	CherryPickDialog: typeof CherryPickDialog;
	InteractiveRebaseDialog: typeof InteractiveRebaseDialog;
	ReflogDialog: typeof ReflogDialog;
	ReflogResetDialog: typeof ReflogResetDialog;
	ResetCommitDialog: typeof ResetCommitDialog;
	RevertDialog: typeof RevertDialog;
};

function createDialog<TName extends keyof DialogModuleMap>(
	load: () => Promise<Record<TName, DialogModuleMap[TName]>>,
	name: TName,
) {
	type Props = ComponentProps<DialogModuleMap[TName]>;
	const loader = createDeferredLoader(() =>
		load().then((module) => module[name] as ComponentType<Props>),
	);
	return function DeferredCommitDialog(props: Props) {
		return (
			<DeferredPrimaryOverlay
				fallback={<SurfaceLoading label="Opening Git operation" overlay />}
				loader={loader}
				props={props}
			/>
		);
	};
}
