import type { ColKey } from "../constants";

export function HeaderCell({
	keyName,
	label,
	onResizeStart,
	showHandle,
}: {
	keyName: ColKey;
	label: string;
	onResizeStart: (
		key: ColKey,
		event: React.PointerEvent<HTMLButtonElement>,
	) => void;
	showHandle: boolean;
}) {
	return (
		<div className="relative overflow-hidden whitespace-nowrap border-r px-2">
			{label}
			{showHandle ? (
				<button
					aria-label={`Resize ${label} column`}
					className="absolute right-0 top-0 h-full w-2 cursor-col-resize bg-transparent hover:bg-accent/50"
					onPointerDown={(event) => onResizeStart(keyName, event)}
					type="button"
				/>
			) : null}
		</div>
	);
}
