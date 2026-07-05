import { Check, Monitor, Moon, Palette, Sun } from "lucide-react";
import type { AppTheme } from "@/generated/types";
import { setSetting, useSetting } from "@/lib/settings/settingsStore";
import { SettingGroup } from "./SettingsControls";

const themeOptions: Array<{
	accent: string;
	background: string;
	card: string;
	description: string;
	foreground: string;
	icon: typeof Sun;
	label: string;
	value: AppTheme;
}> = [
	{
		accent: "#778499",
		background: "linear-gradient(135deg,#f7f9fb 0%,#14171c 100%)",
		card: "#dbe1ea",
		description: "Follows the operating system setting.",
		foreground: "#1d2733",
		icon: Monitor,
		label: "System",
		value: "System",
	},
	{
		accent: "#2f6f8f",
		background: "#f7f9fb",
		card: "#e8eef4",
		description: "Clean, bright, and low-friction.",
		foreground: "#1d2733",
		icon: Sun,
		label: "Morning Glass",
		value: "Morning",
	},
	{
		accent: "#d7dde8",
		background: "#14171c",
		card: "#222833",
		description: "A quiet dark workspace.",
		foreground: "#f2f5f8",
		icon: Moon,
		label: "Midnight Ink",
		value: "Midnight",
	},
	{
		accent: "#197d77",
		background: "#eef8f6",
		card: "#d7ebe8",
		description: "Cool coastal teal with soft contrast.",
		foreground: "#17312f",
		icon: Palette,
		label: "Harbor Mist",
		value: "Harbor",
	},
	{
		accent: "#7fb069",
		background: "#101710",
		card: "#1d2a1d",
		description: "Deep green with readable warm highlights.",
		foreground: "#eef6ea",
		icon: Palette,
		label: "Forest Signal",
		value: "Forest",
	},
	{
		accent: "#d9773f",
		background: "#181412",
		card: "#2a201b",
		description: "Charcoal with amber command accents.",
		foreground: "#f8f0ea",
		icon: Palette,
		label: "Ember Slate",
		value: "Ember",
	},
	{
		accent: "#b85a70",
		background: "#fff7f7",
		card: "#f3e2e5",
		description: "Warm light theme with berry accents.",
		foreground: "#382128",
		icon: Palette,
		label: "Rose Quartz",
		value: "Rose",
	},
	{
		accent: "#8c6b2f",
		background: "#f9f7ef",
		card: "#ebe5d3",
		description: "Paper-like surface with moss and copper.",
		foreground: "#302b1d",
		icon: Palette,
		label: "Copper Moss",
		value: "Copper",
	},
	{
		accent: "#9b86e8",
		background: "#14121d",
		card: "#242033",
		description: "Dark violet balanced with steel neutrals.",
		foreground: "#f2efff",
		icon: Palette,
		label: "Orchid Night",
		value: "Orchid",
	},
];

export function ThemeSettings() {
	const theme = useSetting("Theme");
	const selectedTheme =
		theme === "Light" ? "Morning" : theme === "Dark" ? "Midnight" : theme;

	return (
		<div className="space-y-5">
			<SettingGroup
				description="Choose the colour palette used throughout LovelyGit."
				title="Colour Theme"
			>
				<div className="grid grid-cols-[repeat(auto-fit,minmax(180px,1fr))] gap-3">
					{themeOptions.map((option) => (
						<ThemeCard
							key={option.value}
							option={option}
							selected={selectedTheme === option.value}
						/>
					))}
				</div>
			</SettingGroup>
		</div>
	);
}

function ThemeCard({
	option,
	selected,
}: {
	option: (typeof themeOptions)[number];
	selected: boolean;
}) {
	return (
		<button
			aria-pressed={selected}
			className={`group grid min-h-40 gap-3 rounded-lg border bg-background p-3 text-left transition hover:border-primary/70 hover:bg-accent/40 ${selected ? "border-primary ring-2 ring-ring/35" : ""}`}
			onClick={() => void setSetting("Theme", option.value)}
			type="button"
		>
			<ThemePreview option={option} />
			<span className="flex min-w-0 items-start justify-between gap-2">
				<span className="min-w-0">
					<span className="flex items-center gap-2 font-medium text-sm">
						<option.icon aria-hidden="true" className="size-4" />
						{option.label}
					</span>
					<span className="mt-1 block text-muted-foreground text-xs leading-snug">
						{option.description}
					</span>
				</span>
				<span
					className={`inline-flex size-5 shrink-0 items-center justify-center rounded-full border ${selected ? "border-primary bg-primary text-primary-foreground" : "text-transparent"}`}
				>
					<Check aria-hidden="true" className="size-3.5" />
				</span>
			</span>
		</button>
	);
}

function ThemePreview({ option }: { option: (typeof themeOptions)[number] }) {
	return (
		<div
			className="grid h-20 overflow-hidden rounded-md border"
			style={{
				background: option.background,
				borderColor: option.accent,
				color: option.foreground,
			}}
		>
			<div className="flex items-center gap-1 border-b border-current/15 px-2">
				<span
					className="size-2 rounded-full"
					style={{ backgroundColor: option.accent }}
				/>
				<span className="h-1.5 w-12 rounded-full bg-current/20" />
			</div>
			<div className="grid grid-cols-[1fr_1.6fr] gap-2 p-2">
				<div className="rounded-sm" style={{ backgroundColor: option.card }} />
				<div className="grid content-start gap-1.5">
					<span
						className="h-2 w-20 rounded-full"
						style={{ backgroundColor: option.accent }}
					/>
					<span className="h-1.5 w-24 rounded-full bg-current/25" />
					<span className="h-1.5 w-16 rounded-full bg-current/20" />
				</div>
			</div>
		</div>
	);
}
