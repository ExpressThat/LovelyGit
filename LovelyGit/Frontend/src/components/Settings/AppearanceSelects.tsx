import {
	Select,
	SelectContent,
	SelectItem,
	SelectTrigger,
	SelectValue,
} from "@/components/ui/select";
import { type FontOption, getFontOption } from "@/lib/settings/font/fontUtils";
import {
	getThemeOption,
	type ThemeOption,
} from "@/lib/settings/theme/themeCatalog";

export function ThemeSelect({
	dropdownBoundary,
	onValueChange,
	options,
	value,
}: {
	dropdownBoundary: Element | null;
	onValueChange: (value: string) => void;
	options: ThemeOption[];
	value: string;
}) {
	const selected = getThemeOption(value);
	return (
		<Select
			onValueChange={(nextValue) => {
				if (nextValue) {
					onValueChange(nextValue);
				}
			}}
			value={value}
		>
			<SelectTrigger className="w-72 bg-background/60">
				<SelectValue>
					<Swatch option={selected} />
					<span>{selected.label}</span>
				</SelectValue>
			</SelectTrigger>
			<SelectContent
				align="end"
				alignItemWithTrigger={false}
				className="max-h-80 w-72"
				collisionAvoidance={{
					align: "shift",
					fallbackAxisSide: "none",
					side: "flip",
				}}
				collisionBoundary={dropdownBoundary ?? undefined}
				collisionPadding={12}
			>
				{options.map((option) => (
					<SelectItem key={option.value} value={option.value}>
						<Swatch option={option} />
						<span>{option.label}</span>
					</SelectItem>
				))}
			</SelectContent>
		</Select>
	);
}

export function FontSelect({
	dropdownBoundary,
	onValueChange,
	options,
	value,
}: {
	dropdownBoundary: Element | null;
	onValueChange: (value: string) => void;
	options: FontOption[];
	value: string;
}) {
	const selected = getFontOption(value);
	return (
		<Select
			onValueChange={(nextValue) => {
				if (nextValue) {
					onValueChange(nextValue);
				}
			}}
			value={value}
		>
			<SelectTrigger className="w-72 bg-background/60">
				<SelectValue>{selected.label}</SelectValue>
			</SelectTrigger>
			<SelectContent
				align="end"
				alignItemWithTrigger={false}
				className="max-h-80 w-72"
				collisionAvoidance={{
					align: "shift",
					fallbackAxisSide: "none",
					side: "flip",
				}}
				collisionBoundary={dropdownBoundary ?? undefined}
				collisionPadding={12}
			>
				{options.map((option) => (
					<SelectItem key={option.value} value={option.value}>
						<span style={{ fontFamily: option.stack }}>{option.label}</span>
					</SelectItem>
				))}
			</SelectContent>
		</Select>
	);
}

function Swatch({ option }: { option: ThemeOption }) {
	return (
		<span
			className="inline-flex size-6 items-center justify-center rounded-md font-semibold text-xs"
			style={{
				background: option.variables.background,
				color: option.variables.primary,
			}}
		>
			Aa
		</span>
	);
}
