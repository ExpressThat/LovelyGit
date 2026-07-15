import type { ComponentProps, ComponentType } from "react";
import { SurfaceLoading } from "@/AppLazySurfaces";
import { DeferredPrimaryOverlay } from "@/AppPrimaryOverlays";
import { createDeferredLoader } from "@/lib/deferredLoader";
import type { CreateWorktreeDialog } from "./CreateWorktreeDialog";
import type { LockWorktreeDialog } from "./LockWorktreeDialog";
import type { RemoveWorktreeDialog } from "./RemoveWorktreeDialog";

export const DeferredCreateWorktreeDialog = createDialog(
	() => import("./CreateWorktreeDialog"),
	"CreateWorktreeDialog",
);
export const DeferredLockWorktreeDialog = createDialog(
	() => import("./LockWorktreeDialog"),
	"LockWorktreeDialog",
);
export const DeferredRemoveWorktreeDialog = createDialog(
	() => import("./RemoveWorktreeDialog"),
	"RemoveWorktreeDialog",
);

type DialogModuleMap = {
	CreateWorktreeDialog: typeof CreateWorktreeDialog;
	LockWorktreeDialog: typeof LockWorktreeDialog;
	RemoveWorktreeDialog: typeof RemoveWorktreeDialog;
};

function createDialog<TName extends keyof DialogModuleMap>(
	load: () => Promise<Record<TName, DialogModuleMap[TName]>>,
	name: TName,
) {
	type Props = ComponentProps<DialogModuleMap[TName]>;
	const loader = createDeferredLoader(() =>
		load().then((module) => module[name] as ComponentType<Props>),
	);
	return function DeferredWorktreeDialog(props: Props) {
		return (
			<DeferredPrimaryOverlay
				fallback={<SurfaceLoading label="Opening Git operation" overlay />}
				loader={loader}
				props={props}
			/>
		);
	};
}
