import { motion } from "motion/react";
import { cn } from "@/lib/utils";

export function SkeletonShimmer({ className }: { className: string }) {
	return (
		<span
			aria-hidden="true"
			className={cn(
				"relative inline-block overflow-hidden bg-foreground/12 ring-1 ring-foreground/5",
				className,
			)}
		>
			<motion.span
				className="absolute inset-y-0 -left-1/2 w-1/2 bg-linear-to-r from-transparent via-foreground/25 to-transparent"
				animate={{ x: ["0%", "300%"] }}
				transition={{
					duration: 3.8,
					repeat: Number.POSITIVE_INFINITY,
					ease: "linear",
				}}
			/>
		</span>
	);
}
