import "@testing-library/jest-dom/vitest";
import { cleanup } from "@testing-library/react";
import { afterEach } from "vitest";

afterEach(() => cleanup());

if (typeof window !== "undefined") {
	Object.defineProperty(window, "matchMedia", {
		configurable: true,
		value: (query: string) => ({
			addEventListener: () => undefined,
			dispatchEvent: () => false,
			matches: false,
			media: query,
			onchange: null,
			removeEventListener: () => undefined,
		}),
	});
	window.scrollTo = () => undefined;
}

class ResizeObserverStub implements ResizeObserver {
	disconnect() {}
	observe() {}
	unobserve() {}
}

globalThis.ResizeObserver = ResizeObserverStub;

if (typeof Element !== "undefined") {
	Element.prototype.scrollIntoView = () => undefined;
}
