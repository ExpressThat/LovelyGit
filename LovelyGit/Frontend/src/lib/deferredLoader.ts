export function createDeferredLoader<T>(loadValue: () => Promise<T>) {
	let value: T | null = null;
	let pending: Promise<T> | null = null;
	return {
		get: () => value,
		load: () => {
			if (value) return Promise.resolve(value);
			if (pending) return pending;
			pending = loadValue().then(
				(loaded) => {
					value = loaded;
					return loaded;
				},
				(error: unknown) => {
					pending = null;
					throw error;
				},
			);
			return pending;
		},
	};
}
