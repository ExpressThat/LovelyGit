import { BadgeCheck } from "@/components/icons/lovelyIcons";
import { AnimatePresence, motion, useReducedMotion } from "@/lib/motion";
import { MutationOptionToggle } from "./MutationOptionToggle";

export function CreateTagAnnotationOptions({
	disabled,
	isAnnotated,
	isSigned,
	message,
	onAnnotatedChange,
	onMessageChange,
	onSignedChange,
}: {
	disabled: boolean;
	isAnnotated: boolean;
	isSigned: boolean;
	message: string;
	onAnnotatedChange: (checked: boolean) => void;
	onMessageChange: (message: string) => void;
	onSignedChange: (checked: boolean) => void;
}) {
	const reduceMotion = useReducedMotion();
	return (
		<>
			<MutationOptionToggle
				accessibleName="Annotated tag with a message"
				checked={isAnnotated}
				disabled={disabled}
				id="toggle-annotated-tag"
				onCheckedChange={onAnnotatedChange}
			>
				Annotated tag with a message
			</MutationOptionToggle>
			<AnimatePresence initial={false}>
				{isAnnotated ? (
					<motion.div
						animate={{ height: "auto", opacity: 1, y: 0 }}
						className="grid gap-3 overflow-hidden"
						exit={
							reduceMotion ? { opacity: 0 } : { height: 0, opacity: 0, y: -4 }
						}
						initial={
							reduceMotion ? { opacity: 0 } : { height: 0, opacity: 0, y: -4 }
						}
						transition={{
							duration: reduceMotion ? 0 : 0.2,
							ease: [0.22, 1, 0.36, 1],
						}}
					>
						<label
							className="grid gap-2 font-medium text-sm"
							htmlFor="tag-message"
						>
							Message
							<textarea
								aria-label="Tag message"
								className="min-h-20 resize-y rounded-md border bg-background px-3 py-2 font-normal text-sm outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50"
								disabled={disabled}
								id="tag-message"
								onChange={(event) => onMessageChange(event.currentTarget.value)}
								onInput={(event) => onMessageChange(event.currentTarget.value)}
								value={message}
							/>
						</label>
						<MutationOptionToggle
							accessibleName="Cryptographically sign this tag"
							checked={isSigned}
							disabled={disabled}
							icon={
								<BadgeCheck
									aria-hidden="true"
									className="size-4 text-primary"
								/>
							}
							id="toggle-sign-tag"
							onCheckedChange={onSignedChange}
						>
							Sign with the GPG or SSH key configured in Git
						</MutationOptionToggle>
					</motion.div>
				) : null}
			</AnimatePresence>
		</>
	);
}
