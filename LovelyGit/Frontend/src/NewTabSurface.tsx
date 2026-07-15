import { SurfaceLoading } from "./AppLazySurfaces";
import { DeferredPrimaryOverlay } from "./AppPrimaryOverlays";
import { createDeferredLoader } from "./lib/deferredLoader";

const newTabLoader = createDeferredLoader(() =>
	import("./components/NewTab/NewTab").then((module) => module.NewTab),
);

export function NewTabSurface() {
	return (
		<DeferredPrimaryOverlay
			fallback={<SurfaceLoading label="Opening repositories" />}
			loader={newTabLoader}
			props={{}}
		/>
	);
}
