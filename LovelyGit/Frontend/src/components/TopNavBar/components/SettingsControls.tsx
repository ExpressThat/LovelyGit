import { Button } from "@/components/ui/button";

export function SettingGroup({
	children,
	description,
	title,
}: {
	children: React.ReactNode;
	description: string;
	title: string;
}) {
	return (
		<section className="grid gap-3 border-b pb-5 last:border-b-0 last:pb-0">
			<div>
				<h3 className="text-sm font-semibold">{title}</h3>
				<p className="text-xs text-muted-foreground">{description}</p>
			</div>
			{children}
		</section>
	);
}

export function SegmentedControl({ children }: { children: React.ReactNode }) {
	return (
		<div className="inline-flex rounded-lg border bg-background p-0.5">
			{children}
		</div>
	);
}

export function SegmentedButton({
	icon,
	isActive,
	label,
	onClick,
}: {
	icon: React.ReactNode;
	isActive: boolean;
	label: string;
	onClick: () => void;
}) {
	return (
		<Button
			className="rounded-md"
			onClick={onClick}
			variant={isActive ? "secondary" : "ghost"}
		>
			{icon}
			{label}
		</Button>
	);
}
