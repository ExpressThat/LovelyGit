import type { RemoteAction } from "./RemoteActionsControl";

export function RemoteDefaultRow({
	action,
	isDefault,
	isDisabled,
	onChooseDefault,
	onRun,
}: {
	action: RemoteAction;
	isDefault: boolean;
	isDisabled: boolean;
	onChooseDefault: () => void;
	onRun: () => void;
}) {
	return (
		<div
			className={
				isDefault
					? "flex items-center bg-accent text-accent-foreground"
					: "flex items-center text-foreground hover:bg-accent"
			}
		>
			<label
				aria-label={`Set ${action.menuLabel} as default`}
				className="ml-2 inline-flex size-7 shrink-0 items-center justify-center rounded-full focus-within:ring-2 focus-within:ring-ring"
				title={`Set ${action.menuLabel} as default`}
			>
				<input
					checked={isDefault}
					className="peer sr-only"
					disabled={isDisabled}
					name="remote-primary-action"
					onChange={onChooseDefault}
					type="radio"
					value={action.value}
				/>
				<span className="inline-flex size-4 items-center justify-center rounded-full border-2 border-muted-foreground peer-checked:border-primary">
					{isDefault ? (
						<span className="size-1.5 rounded-full bg-primary" />
					) : null}
				</span>
			</label>
			<button
				className="flex min-h-10 min-w-0 flex-1 items-center gap-2 px-2 text-left text-sm font-medium disabled:pointer-events-none disabled:opacity-50"
				disabled={isDisabled}
				onClick={onRun}
				title={`Run ${action.menuLabel}`}
				type="button"
			>
				<action.icon aria-hidden="true" className="size-4 shrink-0" />
				<span className="truncate">{action.menuLabel}</span>
			</button>
		</div>
	);
}
