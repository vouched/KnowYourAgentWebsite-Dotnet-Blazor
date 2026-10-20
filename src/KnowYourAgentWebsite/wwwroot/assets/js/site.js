// Progressive enhancements. The page works without JavaScript; these only make it nicer.

const RESTART_POLL_INTERVAL_MS = 750;
const RESTART_TIMEOUT_MS = 60_000;

// Show that a form submission is in progress, e.g. while simulated traffic is being sent and delivered to Vouched
document.addEventListener("submit", (event) => {
	const button = event.submitter;
	if (!(button instanceof HTMLButtonElement) || !button.dataset.busyLabel) {
		return;
	}

	button.setAttribute("aria-busy", "true");
	const label = button.querySelector(".btn__label");
	if (label) {
		label.textContent = button.dataset.busyLabel;
	}
});

// After you save your keys, the app restarts itself in-process to apply them.
// Poll its status endpoint until a new instance answers, then reload the page.
function watchForRestart() {
	const panel = document.querySelector("[data-restart-from]");
	if (!panel || panel.dataset.watching === "true") {
		return;
	}

	panel.dataset.watching = "true";
	const previousInstanceId = panel.dataset.restartFrom;
	const statusUrl = panel.dataset.statusUrl;
	const deadline = Date.now() + RESTART_TIMEOUT_MS;

	const poll = async () => {
		try {
			const response = await fetch(statusUrl, { cache: "no-store", headers: { accept: "application/json" } });
			if (response.ok) {
				const status = await response.json();
				if (status.instanceId !== previousInstanceId) {
					// A hash-only change scrolls instead of reloading, so set the anchor first and then reload to load the new keys
					window.history.replaceState(null, "", status.hasProjectId ? "#simulate" : "#connect");
					window.location.reload();
					return;
				}
			}
		} catch {
			// Expected while the app is restarting
		}

		if (Date.now() > deadline) {
			panel.dataset.state = "timeout";
			return;
		}

		window.setTimeout(poll, RESTART_POLL_INTERVAL_MS);
	};

	window.setTimeout(poll, RESTART_POLL_INTERVAL_MS);
}

watchForRestart();

// Blazor's enhanced navigation and form handling update the page without re-running scripts
window.Blazor?.addEventListener?.("enhancedload", watchForRestart);
