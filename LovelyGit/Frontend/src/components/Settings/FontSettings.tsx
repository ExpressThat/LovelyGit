import { Check, Type } from "lucide-react";
import { type FontOption, fontOptions } from "@/lib/settings/font/fontUtils";
import { setSetting, useSetting } from "@/lib/settings/settingsStore";
import { SettingGroup } from "./SettingsControls";

export function FontSettings() {
	const font = useSetting("Font");

	return (
		<div className="space-y-5">
			<SettingGroup
				description="Choose the typeface used throughout LovelyGit."
				title="Font"
			>
				<div className="grid grid-cols-[repeat(auto-fit,minmax(180px,1fr))] gap-3">
					{fontOptions.map((option) => (
						<FontCard
							key={option.value}
							option={option}
							selected={font === option.value}
						/>
					))}
				</div>
			</SettingGroup>
		</div>
	);
}

function FontCard({
	option,
	selected,
}: {
	option: FontOption;
	selected: boolean;
}) {
	return (
		<button
			aria-pressed={selected}
			className={`grid min-h-40 gap-3 rounded-lg border bg-background p-3 text-left transition hover:border-primary/70 hover:bg-accent/40 ${selected ? "border-primary ring-2 ring-ring/35" : ""}`}
			onClick={() => void setSetting("Font", option.value)}
			type="button"
		>
			<div
				className="grid h-20 content-center gap-2 rounded-md border bg-card px-3"
				style={{ fontFamily: option.stack }}
			>
				<span className="truncate font-semibold text-2xl">{option.sample}</span>
				<span className="truncate text-muted-foreground text-xs">
					Aa Bb Cc 123 ./git
				</span>
			</div>
			<span className="flex min-w-0 items-start justify-between gap-2">
				<span className="min-w-0">
					<span className="flex items-center gap-2 font-medium text-sm">
						<Type aria-hidden="true" className="size-4" />
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
