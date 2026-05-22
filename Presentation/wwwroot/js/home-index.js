// *************************************************************************************************
// home-index.js — Home page Upcoming Events (`Index.cshtml`)
// *************************************************************************************************
// Uses `EVENTS` from `events-portal.js` and renders vertical cards into `[data-home-upcoming]`.

// ================================================================================================
// Upcoming events segment (week / month / year)
// ================================================================================================

/**
 * @returns {void}
 */
function initHomeUpcomingSegment() {
  const block = document.querySelector("[data-home-upcoming]");
  if (!block || typeof EVENTS === "undefined") return;

  const buttons = block.querySelectorAll("[data-event-range]");
  const empty = block.querySelector("[data-event-empty]");
  const eventsList = block.querySelector("[data-events-list]");

  if (!buttons.length || !empty || !eventsList) return;

  /**
   * @param {string} range
   * @returns {Promise<void>}
   */
  async function applyRange(range) {
    const key = range === "week" || range === "month" || range === "year" ? range : "month";
    const { fromMs, toMs } = homeTimeRangeBoundsMs(key);

    const matches = EVENTS.filter((e) => {
      if (e.timeScope === "past") return false;
      const ms = eventDateTimeMs(e);
      if (ms == null) return false;
      if (ms < fromMs) return false;
      return ms <= toMs;
    }).sort((a, b) => (eventDateTimeMs(a) ?? 0) - (eventDateTimeMs(b) ?? 0));

    eventsList.innerHTML = "";
    await Promise.all(matches.map((ev) => appendHomeVerticalEventCard(eventsList, ev.id)));

    const hasAny = matches.length > 0;
    empty.hidden = hasAny;
    eventsList.hidden = !hasAny;

    buttons.forEach((btn) => {
      const on = btn.getAttribute("data-event-range") === key;
      btn.classList.toggle("active", on);
      btn.setAttribute("aria-pressed", on ? "true" : "false");
    });
  }

  buttons.forEach((btn) => {
    btn.addEventListener("click", () => {
      const range = btn.getAttribute("data-event-range");
      if (range) applyRange(range);
    });
  });

  const active = block.querySelector(".segmented-control-option.active[data-event-range]");
  applyRange(active?.getAttribute("data-event-range") || "month");
}

/********************************************************************************/

document.addEventListener("DOMContentLoaded", () => {
  if (document.querySelector("[data-home-upcoming]")) {
    initHomeUpcomingSegment();
  }
});

/********************************************************************************/
