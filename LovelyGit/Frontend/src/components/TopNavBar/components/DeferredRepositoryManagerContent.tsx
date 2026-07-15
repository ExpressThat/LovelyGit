import type { ComponentProps, ComponentType } from "react";
import { SurfaceLoading } from "@/AppLazySurfaces";
import { DeferredPrimaryOverlay } from "@/AppPrimaryOverlays";
import { createDeferredLoader } from "@/lib/deferredLoader";
import type { LfsManagerContent } from "./LfsManagerContent";
import type { SparseCheckoutManagerContent } from "./SparseCheckoutManagerContent";
import type { SubmoduleManagerContent } from "./SubmoduleManagerContent";

export const DeferredLfsManagerContent = createManagerContent(
	() => import("./LfsManagerContent"),
	"LfsManagerContent",
	"Opening Git LFS",
);
export const DeferredSparseCheckoutManagerContent = createManagerContent(
	() => import("./SparseCheckoutManagerContent"),
	"SparseCheckoutManagerContent",
	"Opening sparse checkout",
);
export const DeferredSubmoduleManagerContent = createManagerContent(
	() => import("./SubmoduleManagerContent"),
	"SubmoduleManagerContent",
	"Opening submodules",
);

type ManagerModuleMap = {
	LfsManagerContent: typeof LfsManagerContent;
	SparseCheckoutManagerContent: typeof SparseCheckoutManagerContent;
	SubmoduleManagerContent: typeof SubmoduleManagerContent;
};

function createManagerContent<TName extends keyof ManagerModuleMap>(
	load: () => Promise<Record<TName, ManagerModuleMap[TName]>>,
	name: TName,
	loadingLabel: string,
) {
	type Props = ComponentProps<ManagerModuleMap[TName]>;
	const loader = createDeferredLoader(() =>
		load().then((module) => module[name] as ComponentType<Props>),
	);
	return function DeferredManagerContent(props: Props) {
		return (
			<DeferredPrimaryOverlay
				fallback={<SurfaceLoading label={loadingLabel} overlay />}
				loader={loader}
				props={props}
			/>
		);
	};
}
