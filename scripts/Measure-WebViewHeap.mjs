const port = Number.parseInt(process.argv[2] ?? "9333", 10);
const targets = await fetch(`http://127.0.0.1:${port}/json`).then((response) =>
	response.json(),
);
const target = targets.find((entry) => entry.title === "LovelyGit");
if (!target) throw new Error("LovelyGit WebView target was not found.");

const socket = new WebSocket(target.webSocketDebuggerUrl);
let nextId = 0;
const pending = new Map();
socket.onmessage = ({ data }) => {
	const message = JSON.parse(data);
	const request = pending.get(message.id);
	if (!request) return;
	pending.delete(message.id);
	message.error ? request.reject(message.error) : request.resolve(message.result);
};
await new Promise((resolve, reject) => {
	socket.onopen = resolve;
	socket.onerror = reject;
});
const send = (method, params = {}) =>
	new Promise((resolve, reject) => {
		const id = ++nextId;
		pending.set(id, { reject, resolve });
		socket.send(JSON.stringify({ id, method, params }));
	});

await send("HeapProfiler.enable");
await send("HeapProfiler.collectGarbage");
const result = await send("Runtime.evaluate", {
	expression:
		"({usedJsHeapBytes:performance.memory.usedJSHeapSize,totalJsHeapBytes:performance.memory.totalJSHeapSize})",
	returnByValue: true,
});
console.log(JSON.stringify(result.result.value));
socket.close();
