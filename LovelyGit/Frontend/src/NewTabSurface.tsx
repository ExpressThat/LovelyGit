import { lazy, Suspense } from "react";
import { SurfaceLoading } from "./AppLazySurfaces";

const NewTab = lazy(() =>
	import("./components/NewTab/NewTab").then((module) => ({
		default: module.NewTab,
	})),
);

export function NewTabSurface() {
	return (
		<Suspense fallback={<SurfaceLoading label="Opening repositories" />}>
			<NewTab />
		</Suspense>
	);
}
