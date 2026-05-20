// *************************************************************************************************
// events-list-segment.js — Upcoming / Past panel toggle (server-rendered lists)
// *************************************************************************************************

document.addEventListener("DOMContentLoaded", () => {
  initEventsListSegment("[data-allevents-list]", "allevents");
  initEventsListSegment("[data-myevents-list]", "myevents");
});

/********************************************************************************/

/**
 * @param {string} rootSelector
 * @param {string} prefix
 * @returns {void}
 */
function initEventsListSegment(rootSelector, prefix) {
  const root = document.querySelector(rootSelector);
  if (!root) return;

  const buttons = root.querySelectorAll(`[data-${prefix}-range]`);
  const upcoming = root.querySelector(`[data-${prefix}-panel="upcoming"]`);
  const past = root.querySelector(`[data-${prefix}-panel="past"]`);
  if (!buttons.length) return;

  /**
   * @param {string} range
   */
  function applyRange(range) {
    buttons.forEach((btn) => {
      const on = btn.getAttribute(`data-${prefix}-range`) === range;
      btn.classList.toggle("active", on);
      btn.setAttribute("aria-pressed", on ? "true" : "false");
    });
    if (upcoming) upcoming.hidden = range !== "upcoming";
    if (past) past.hidden = range !== "past";
  }

  buttons.forEach((btn) => {
    btn.addEventListener("click", () => {
      const range = btn.getAttribute(`data-${prefix}-range`);
      if (range === "upcoming" || range === "past") applyRange(range);
    });
  });

  const active = root.querySelector(`.segmented-control-option.active[data-${prefix}-range]`);
  applyRange(active?.getAttribute(`data-${prefix}-range`) || "upcoming");
}

/********************************************************************************/
