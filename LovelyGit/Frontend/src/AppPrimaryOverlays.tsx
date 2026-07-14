import {
	type ComponentProps,
	type ComponentType,
	type ReactNode,
	useEffect,
	useState,
} from "react";
import type { CommandPalette as CommandPaletteComponent } from "./components/CommandPalette/CommandPalette";
import type { SettingsDialog as SettingsDialogComponent } from "./components/Settings/SettingsDialog";
import { createDeferredLoader } from "./lib/deferredLoader";

type CommandPaletteProps = ComponentProps<typeof CommandPaletteComponent>;
type SettingsDialogProps = NonNullable<
	ComponentProps<typeof SettingsDialogComponent>
>;

const commandPaletteLoader = createDeferredLoader(() =>
	import("./components/CommandPalette/CommandPalette").then(
		(module) => module.CommandPalette,
	),
);
const settingsDialogLoader = createDeferredLoader(() =>
	import("./components/Settings/SettingsDialog").then(
		(module) => module.SettingsDialog as ComponentType<SettingsDialogProps>,
	),
);

export function PrimaryCommandPalette(props: CommandPaletteProps) {
	return (
		<DeferredPrimaryOverlay
			fallback={<PrimaryOverlayLoading label="Opening command palette" />}
			loader={commandPaletteLoader}
			props={props}
		/>
	);
}

export function PrimarySettingsDialog(props: SettingsDialogProps) {
	return (
		<DeferredPrimaryOverlay
			fallback={<PrimaryOverlayLoading label="Opening settings" />}
			loader={settingsDialogLoader}
			props={props}
		/>
	);
}

export function DeferredPrimaryOverlay<TProps extends object>({
	fallback,
	loader,
	props,
}: {
	fallback: ReactNode;
	loader: DeferredComponentLoader<TProps>;
	props: TProps;
}) {
	const [Component, setComponent] = useState<ComponentType<TProps> | null>(() =>
		loader.get(),
	);
	const [failed, setFailed] = useState(false);
	const [attempt, setAttempt] = useState(0);
	useEffect(() => {
		void attempt;
		if (Component) return;
		let active = true;
		loader.load().then(
			(loaded) => {
				if (active) setComponent(() => loaded);
			},
			() => {
				if (active) setFailed(true);
			},
		);
		return () => {
			active = false;
		};
	}, [Component, loader, attempt]);
	if (Component) return <Component {...props} />;
	if (!failed) return fallback;
	return (
		<div className="fixed inset-0 z-50 grid place-items-center bg-background/70">
			<button
				className="rounded-md border bg-popover px-4 py-2 text-foreground shadow-lg hover:bg-accent"
				onClick={() => {
					setFailed(false);
					setAttempt((value) => value + 1);
				}}
				type="button"
			>
				Retry opening tool
			</button>
		</div>
	);
}

function PrimaryOverlayLoading({ label }: { label: string }) {
	return (
		<div className="fixed inset-0 z-50 grid place-items-center bg-background/70 text-sm text-muted-foreground">
			{label}…
		</div>
	);
}

export type DeferredComponentLoader<TProps extends object> = {
	get: () => ComponentType<TProps> | null;
	load: () => Promise<ComponentType<TProps>>;
};
