import type { ThemeOption } from "@/lib/settings/theme/themeCatalog";

export function ModePreview({
	active,
	label,
	onSelect,
	type,
}: {
	active: boolean;
	label: string;
	onSelect: () => void;
	type: "dark" | "light" | "system";
}) {
	return (
		<button className="grid gap-2 text-center" onClick={onSelect} type="button">
			<div
				className={`relative h-24 overflow-hidden rounded-xl border bg-card transition ${active ? "border-primary ring-2 ring-ring/35" : "border-border"}`}
			>
				{type === "system" ? (
					<div className="absolute inset-0 grid grid-cols-2">
						<div className="bg-[#f3f4f6]" />
						<div className="bg-[#171923]" />
					</div>
				) : null}
				<div
					className={`absolute inset-0 ${type === "light" ? "bg-[#f7f7f8]" : ""} ${type === "dark" ? "bg-[#4a4a48]" : ""}`}
				/>
				<div className="absolute inset-x-4 bottom-0 h-14 rounded-t-lg bg-background/90 p-3 shadow-sm">
					<div className="mx-auto mb-3 h-2 w-20 rounded-full bg-muted-foreground/25" />
					<div className="grid gap-2">
						<div className="h-2 rounded-full bg-muted-foreground/20" />
						<div className="h-2 w-2/3 rounded-full bg-muted-foreground/20" />
					</div>
				</div>
			</div>
			<span className={active ? "text-foreground" : "text-muted-foreground"}>
				{label}
			</span>
		</button>
	);
}

export function CodePreview({
	codeFont,
	darkTheme,
	lightTheme,
}: {
	codeFont: string;
	darkTheme: ThemeOption;
	lightTheme: ThemeOption;
}) {
	return (
		<div
			className="grid overflow-hidden rounded-lg border text-xs"
			style={{ fontFamily: codeFont }}
		>
			<div className="grid grid-cols-2">
				<CodePane option={lightTheme} side="left" />
				<CodePane option={darkTheme} side="right" />
			</div>
			<div className="flex h-5 items-center justify-between bg-muted px-2">
				<span className="size-3 rounded-full bg-muted-foreground/20" />
				<span className="size-3 rounded-full bg-muted-foreground/20" />
			</div>
		</div>
	);
}

function CodePane({
	option,
	side,
}: {
	option: ThemeOption;
	side: "left" | "right";
}) {
	return (
		<div
			className="grid grid-cols-[3rem_1fr] overflow-hidden"
			style={{
				background: option.variables.background,
				color: option.variables.foreground,
			}}
		>
			<div className="grid gap-1 border-r border-current/10 py-2 text-right text-muted-foreground">
				{[1, 2, 3, 4, 5].map((line) => (
					<span className="px-3" key={line}>
						{line}
					</span>
				))}
			</div>
			<div className="grid gap-1 py-2">
				<CodeLine text="const themePreview: ThemeConfig = {" />
				<CodeLine
					highlight={side === "left" ? "remove" : "add"}
					text={`surface: "${side === "left" ? "sidebar" : "sidebar-elevated"}",`}
				/>
				<CodeLine
					highlight={side === "left" ? "remove" : "add"}
					text={`accent: "${option.accent}",`}
				/>
				<CodeLine
					highlight={side === "left" ? "remove" : "add"}
					text={`contrast: ${option.isDark ? 68 : 42},`}
				/>
				<CodeLine text="};" />
			</div>
		</div>
	);
}

function CodeLine({
	highlight,
	text,
}: {
	highlight?: "add" | "remove";
	text: string;
}) {
	return (
		<div
			className={`px-3 ${highlight === "add" ? "bg-green-500/20 text-green-500" : ""} ${highlight === "remove" ? "bg-red-500/20 text-red-500" : ""}`}
		>
			{text}
		</div>
	);
}
