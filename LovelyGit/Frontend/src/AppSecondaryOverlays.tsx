import type { ComponentProps, ComponentType, JSX } from "react";
import {
	type DeferredComponentLoader,
	DeferredPrimaryOverlay,
} from "./AppPrimaryOverlays";
import type { CommitSearchDialog as CommitSearchComponent } from "./components/CommitSearch/CommitSearchDialog";
import type { FileHistoryDialog as FileHistoryComponent } from "./components/FileHistory/FileHistoryDialog";
import type { StashDialog as StashComponent } from "./components/WorkingChanges/StashDialog";
import { createDeferredLoader } from "./lib/deferredLoader";
import {
	type FileBlameDialogProps,
	fileBlameDialogLoader,
} from "./lib/fileBlameDialogLoader";

type CommitSearchProps = ComponentProps<typeof CommitSearchComponent>;
type FileHistoryProps = ComponentProps<typeof FileHistoryComponent>;
type StashProps = NonNullable<ComponentProps<typeof StashComponent>>;

const commitSearchLoader = createDeferredLoader(() =>
	import("./components/CommitSearch/CommitSearchDialog").then(
		(module) => module.CommitSearchDialog,
	),
);
const fileHistoryLoader = createDeferredLoader(() =>
	import("./components/FileHistory/FileHistoryDialog").then(
		(module) => module.FileHistoryDialog,
	),
);
const stashLoader = createDeferredLoader(() =>
	import("./components/WorkingChanges/StashDialog").then(
		(module) => module.StashDialog as ComponentType<StashProps>,
	),
);

export const DeferredCommitSearchDialog = createOverlay<CommitSearchProps>(
	commitSearchLoader,
	"Opening commit search",
);
export const DeferredFileBlameDialog = createOverlay<FileBlameDialogProps>(
	fileBlameDialogLoader,
	"Opening file blame",
);
export const DeferredFileHistoryDialog = createOverlay<FileHistoryProps>(
	fileHistoryLoader,
	"Opening file history",
);
export const DeferredStashDialog = createOverlay<StashProps>(
	stashLoader,
	"Opening stashes",
);

function createOverlay<TProps extends object>(
	loader: DeferredComponentLoader<TProps>,
	label: string,
) {
	return function DeferredOverlay(props: TProps): JSX.Element {
		return (
			<DeferredPrimaryOverlay
				fallback={<SecondaryOverlayLoading label={label} />}
				loader={loader}
				props={props}
			/>
		);
	};
}

function SecondaryOverlayLoading({ label }: { label: string }) {
	return (
		<div className="fixed inset-0 z-50 grid place-items-center bg-background/70 text-sm text-muted-foreground">
			{label}…
		</div>
	);
}
