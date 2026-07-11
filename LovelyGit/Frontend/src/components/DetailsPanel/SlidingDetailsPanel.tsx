import { AnimatePresence, motion } from "motion/react";
import type { ReactNode } from "react";
import { X } from "@/components/icons/lovelyIcons";
import { HorizontalPanelHandle } from "@/components/layout/HorizontalPanelHandle";
import { useHorizontalPanelResize } from "@/components/layout/useHorizontalPanelResize";
import { setSetting, useSetting } from "@/lib/settings/settingsStore";

export function SlidingDetailsPanel({
	children,
	isOpen,
	onClose,
	title,
}: {
	children: ReactNode;
	isOpen: boolean;
	onClose: () => void;
	title: string;
}) {
	const savedWidth = useSetting("DetailsPanelWidth");
	const resize = useHorizontalPanelResize({
		direction: -1,
		max: 800,
		min: 320,
		onCommit: (width) => void setSetting("DetailsPanelWidth", width),
		width: savedWidth,
	});
	return (
		<AnimatePresence>
			{isOpen ? (
				<motion.aside
					animate={{ flexBasis: `${resize.width}px`, opacity: 1 }}
					className="relative z-10 flex h-full shrink-0 overflow-hidden border-l bg-popover text-popover-foreground shadow-xl"
					exit={{ flexBasis: "0px", opacity: 0 }}
					initial={{ flexBasis: "0px", opacity: 0 }}
					transition={{ duration: 0.22, ease: [0.22, 1, 0.36, 1] }}
				>
					<HorizontalPanelHandle
						label="Resize details panel"
						onPointerDown={resize.startResize}
						onResizeBy={(amount) => resize.resizeBy(-amount)}
						side="left"
					/>
					<div
						className="flex h-full shrink-0 flex-col"
						style={{ width: resize.width }}
					>
						<header className="flex h-11 shrink-0 items-center justify-between border-b px-3">
							<h2 className="truncate text-sm font-semibold leading-none text-foreground">
								{title}
							</h2>
							<button
								aria-label="Close"
								className="inline-flex size-7 items-center justify-center rounded-md text-muted-foreground hover:bg-accent hover:text-accent-foreground"
								onClick={onClose}
								type="button"
							>
								<X aria-hidden="true" size={15} />
							</button>
						</header>
						<div className="custom-scrollbar min-h-0 flex-1 overflow-y-auto">
							{children}
						</div>
					</div>
				</motion.aside>
			) : null}
		</AnimatePresence>
	);
}
