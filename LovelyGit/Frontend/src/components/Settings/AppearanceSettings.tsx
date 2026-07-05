import { CaseSensitive, SwatchBook } from "lucide-react";
import { useState } from "react";
import { Button } from "@/components/ui/button";
import { FontSettings } from "./FontSettings";
import { ThemeSettings } from "./ThemeSettings";

type AppearanceSection = "theme" | "font";

const sections: Array<{
	icon: typeof SwatchBook;
	id: AppearanceSection;
	label: string;
}> = [
	{ icon: SwatchBook, id: "theme", label: "Theme" },
	{ icon: CaseSensitive, id: "font", label: "Font" },
];

export function AppearanceSettings() {
	const [activeSection, setActiveSection] =
		useState<AppearanceSection>("theme");

	return (
		<div className="grid gap-5">
			<div className="inline-flex w-fit rounded-lg border bg-background p-0.5">
				{sections.map((section) => (
					<Button
						className="rounded-md"
						key={section.id}
						onClick={() => setActiveSection(section.id)}
						variant={activeSection === section.id ? "secondary" : "ghost"}
					>
						<section.icon aria-hidden="true" className="size-4" />
						{section.label}
					</Button>
				))}
			</div>
			{activeSection === "theme" ? <ThemeSettings /> : null}
			{activeSection === "font" ? <FontSettings /> : null}
		</div>
	);
}
