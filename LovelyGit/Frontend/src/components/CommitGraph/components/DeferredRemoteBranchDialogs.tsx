import type { ComponentProps, ComponentType } from "react";
import { SurfaceLoading } from "@/AppLazySurfaces";
import { DeferredPrimaryOverlay } from "@/AppPrimaryOverlays";
import { createDeferredLoader } from "@/lib/deferredLoader";
import type {
	CheckoutRemoteBranchDialog,
	DeleteRemoteBranchDialog,
} from "./RemoteBranchDialogs";

export const DeferredCheckoutRemoteBranchDialog = createDialog(
	"CheckoutRemoteBranchDialog",
);
export const DeferredDeleteRemoteBranchDialog = createDialog(
	"DeleteRemoteBranchDialog",
);

type DialogModuleMap = {
	CheckoutRemoteBranchDialog: typeof CheckoutRemoteBranchDialog;
	DeleteRemoteBranchDialog: typeof DeleteRemoteBranchDialog;
};

function createDialog<TName extends keyof DialogModuleMap>(name: TName) {
	type Props = ComponentProps<DialogModuleMap[TName]>;
	const loader = createDeferredLoader(() =>
		import("./RemoteBranchDialogs").then(
			(module) => module[name] as ComponentType<Props>,
		),
	);
	return function DeferredRemoteBranchDialog(props: Props) {
		return (
			<DeferredPrimaryOverlay
				fallback={<SurfaceLoading label="Opening Git operation" overlay />}
				loader={loader}
				props={props}
			/>
		);
	};
}
