import type { ComponentProps, ComponentType } from "react";
import { SurfaceLoading } from "@/AppLazySurfaces";
import { DeferredPrimaryOverlay } from "@/AppPrimaryOverlays";
import { createDeferredLoader } from "@/lib/deferredLoader";
import type { CheckoutTagDialog } from "./CheckoutTagDialog";
import type { CreateTagDialog } from "./CreateTagDialog";
import type { DeleteTagDialog } from "./DeleteTagDialog";

export const DeferredCheckoutTagDialog = createDialog(
	() => import("./CheckoutTagDialog"),
	"CheckoutTagDialog",
);
export const DeferredCreateTagDialog = createDialog(
	() => import("./CreateTagDialog"),
	"CreateTagDialog",
);
export const DeferredDeleteTagDialog = createDialog(
	() => import("./DeleteTagDialog"),
	"DeleteTagDialog",
);

type DialogModuleMap = {
	CheckoutTagDialog: typeof CheckoutTagDialog;
	CreateTagDialog: typeof CreateTagDialog;
	DeleteTagDialog: typeof DeleteTagDialog;
};

function createDialog<TName extends keyof DialogModuleMap>(
	load: () => Promise<Record<TName, DialogModuleMap[TName]>>,
	name: TName,
) {
	type Props = ComponentProps<DialogModuleMap[TName]>;
	const loader = createDeferredLoader(() =>
		load().then((module) => module[name] as ComponentType<Props>),
	);
	return function DeferredTagDialog(props: Props) {
		return (
			<DeferredPrimaryOverlay
				fallback={<SurfaceLoading label="Opening Git operation" overlay />}
				loader={loader}
				props={props}
			/>
		);
	};
}
