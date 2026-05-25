// *************************************************************************************************
// events-list-segment.js — Upcoming / Past panel toggle (server-rendered lists)
// *************************************************************************************************

// - Shared segmented control for All Events and My Events when lists are server-rendered (HTML already in the page).
// - Does not fetch cards; only toggles which `[data-*-panel]` is visible and syncs pill state.

// *************************************************************************************************
// Page bootstrap
// *************************************************************************************************

document.addEventListener("DOMContentLoaded", () => {
  initEventsListSegment("[data-allevents-list]", "allevents");
  initEventsListSegment("[data-myevents-list]", "myevents");
});

/********************************************************************************/

// *************************************************************************************************
// Segmented control (Upcoming ↔ Past)
// *************************************************************************************************

// Wires range pills on a list root so clicking one shows the matching upcoming or past panel.
// @param {string} rootSelector CSS selector for the list section root
// @param {string} prefix Data-attribute prefix (`allevents` or `myevents`)
// @returns {void}
function initEventsListSegment(rootSelector, prefix) {
  const root = document.querySelector(rootSelector);
  if (!root) return;

  const buttons = root.querySelectorAll(`[data-${prefix}-range]`);
  const upcoming = root.querySelector(`[data-${prefix}-panel="upcoming"]`);
  const past = root.querySelector(`[data-${prefix}-panel="past"]`);
  if (!buttons.length) return;

  // @param {string} range
  // @returns {void}
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
