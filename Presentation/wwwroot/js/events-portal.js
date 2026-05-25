// *************************************************************************************************
// events-portal.js — Portal event lists (My Events; client-rendered panels)
// *************************************************************************************************

// - Reads `#portal-events-data` for ids and sort/filter metadata, then fetches each card as HTML
//   from `GET /events/card/{eventId}` (`EventsController.GetSingleEventCard`).
// - Exposes shared helpers (`EVENTS`, `eventDateTimeMs`, card appenders) consumed by `home-index.js`.

let EVENTS = [];

// *************************************************************************************************
// Portal events bootstrap
// *************************************************************************************************

// Parses embedded JSON into `EVENTS` and recomputes upcoming/past scopes from real datetimes.
// @returns {void}
function bootstrapPortalEvents() {
  const el = document.getElementById("portal-events-data");
  if (!el?.textContent?.trim()) {
    EVENTS = [];
    return;
  }

  try {
    const parsed = JSON.parse(el.textContent);
    EVENTS = Array.isArray(parsed) ? parsed.map(normalizePortalEvent) : [];
    syncEventTimeScopes();
  } catch (err) {
    console.error("Rodda: failed to parse portal events JSON", err);
    EVENTS = [];
  }
}

/********************************************************************************/

// Maps server DTO fields to the slim client shape list scripts expect.
// @param {Record<string, unknown>} raw
// @returns {{ id: string, creator: string, myEventsRole: string, myAttendanceStatus: string, eventDateIso: string, eventTime24: string, timeScope: "past" | "upcoming" }}
function normalizePortalEvent(raw) {
  return {
    id: String(raw?.id || "").trim(),
    creator: String(raw?.creator || "").trim(),
    myEventsRole: String(raw?.myEventsRole || "").trim(),
    myAttendanceStatus: String(raw?.myAttendanceStatus || "").trim(),
    eventDateIso: String(raw?.eventDateIso || "").trim(),
    eventTime24: String(raw?.eventTime24 || "").trim(),
    timeScope: raw?.timeScope === "past" ? "past" : "upcoming",
  };
}

/********************************************************************************/

// Best-effort event datetime from ISO date + optional 24h time; null when date is missing.
// @param {{ eventDateIso?: string, eventTime24?: string }} event
// @returns {number | null}
function eventDateTimeMs(event) {
  const iso = String(event?.eventDateIso || "").trim();
  if (/^\d{4}-\d{2}-\d{2}$/.test(iso)) {
    const t = String(event?.eventTime24 || "").trim();
    const hhmm = /^\d{2}:\d{2}$/.test(t) ? t : "23:59";
    const d = new Date(`${iso}T${hhmm}:00`);
    return Number.isNaN(d.getTime()) ? null : d.getTime();
  }
  return null;
}

/********************************************************************************/

// Reclassifies each row as past/upcoming relative to now so segmented lists stay accurate.
// @returns {void}
function syncEventTimeScopes() {
  const now = Date.now();
  EVENTS.forEach((ev) => {
    const ms = eventDateTimeMs(ev);
    if (ms == null) return;
    ev.timeScope = ms < now ? "past" : "upcoming";
  });
}

/********************************************************************************/

// Collapses role strings for My Events ownership checks (`owner`, `co owner`, etc.).
// @param {{ myEventsRole?: string }} event
// @returns {string}
function normalizeMyEventsRoleKey(event) {
  return String(event?.myEventsRole || "")
    .trim()
    .toLowerCase()
    .replace(/[^a-z]+/g, " ")
    .trim();
}

/********************************************************************************/

// *************************************************************************************************
// Event card fetching
// *************************************************************************************************

// Fetches a horizontal card partial for one event id.
// @param {string} eventId
// @param {{ fromMyEvents?: boolean }} [options]
// @returns {Promise<string>}
async function fetchHorizontalEventCardHtml(eventId, options) {
  const id = encodeURIComponent(String(eventId || "").trim());
  if (!id) return "";

  let url = `/events/card/${id}`;
  if (options?.fromMyEvents) url += "?fromMyEvents=true";

  const res = await fetch(url, {
    credentials: "same-origin",
    headers: { Accept: "text/html" },
  });
  if (!res.ok) return "";
  return res.text();
}

/********************************************************************************/

// Appends one horizontal card `<li>` when fetch returns HTML.
// @param {HTMLElement} ul
// @param {string} eventId
// @param {{ fromMyEvents?: boolean }} [options]
// @returns {Promise<void>}
async function appendHorizontalEventCard(ul, eventId, options) {
  const html = (await fetchHorizontalEventCardHtml(eventId, options)).trim();
  if (!html || !(ul instanceof HTMLElement)) return;

  const li = document.createElement("li");
  li.className = "events-list-item";
  li.innerHTML = html;
  ul.appendChild(li);
}

/********************************************************************************/

// Fetches the home vertical card layout for one event id.
// @param {string} eventId
// @returns {Promise<string>}
async function fetchVerticalEventCardHtml(eventId) {
  const id = encodeURIComponent(String(eventId || "").trim());
  if (!id) return "";

  const res = await fetch(`/events/card/${id}?layout=vertical`, {
    credentials: "same-origin",
    headers: { Accept: "text/html" },
  });
  if (!res.ok) return "";
  return res.text();
}

/********************************************************************************/

// Appends one vertical home card `<li>` when fetch returns HTML.
// @param {HTMLElement} ul
// @param {string} eventId
// @returns {Promise<void>}
async function appendHomeVerticalEventCard(ul, eventId) {
  const html = (await fetchVerticalEventCardHtml(eventId)).trim();
  if (!html || !(ul instanceof HTMLElement)) return;

  const li = document.createElement("li");
  li.className = "home-event-item";
  li.innerHTML = html;
  ul.appendChild(li);
}

/********************************************************************************/

// Calendar window for Home segments: start of today through end of week / month / year.
// @param {"week" | "month" | "year"} range
// @returns {{ fromMs: number, toMs: number }}
function homeTimeRangeBoundsMs(range) {
  const now = new Date();
  const startOfDayMs = (d) => new Date(d.getFullYear(), d.getMonth(), d.getDate()).getTime();
  const fromMs = startOfDayMs(now);
  let toMs = Number.POSITIVE_INFINITY;

  if (range === "week") {
    toMs = startOfDayMs(new Date(now.getFullYear(), now.getMonth(), now.getDate() + 7));
  } else if (range === "month") {
    toMs = new Date(now.getFullYear(), now.getMonth() + 1, 0, 23, 59, 59, 999).getTime();
  } else if (range === "year") {
    toMs = new Date(now.getFullYear(), 11, 31, 23, 59, 59, 999).getTime();
  }

  return { fromMs, toMs };
}

// *************************************************************************************************
// All Events list (client-rendered only when lists are empty)
// *************************************************************************************************

// Stable comparator that pushes events with no parseable date to the bottom regardless of direction.
// @param {"asc" | "desc"} direction
// @returns {(a: typeof EVENTS[number], b: typeof EVENTS[number]) => number}
function alleventsSortByDate(direction) {
  const factor = direction === "desc" ? -1 : 1;
  return (a, b) => {
    const aMs = eventDateTimeMs(a);
    const bMs = eventDateTimeMs(b);
    if (aMs == null && bMs == null) return 0;
    if (aMs == null) return 1;
    if (bMs == null) return -1;
    return (aMs - bMs) * factor;
  };
}

/********************************************************************************/

// Fetches horizontal cards into empty All Events panels (skips when server already rendered rows).
// @param {HTMLElement} root
// @returns {Promise<void>}
async function renderAlleventsLists(root) {
  const upcomingUl = root.querySelector(
    '[data-allevents-panel="upcoming"] [data-allevents-list-items="upcoming"]'
  );
  const pastUl = root.querySelector('[data-allevents-panel="past"] [data-allevents-list-items="past"]');
  if (!upcomingUl || !pastUl) return;
  if (upcomingUl.children.length > 0 || pastUl.children.length > 0) return;

  upcomingUl.innerHTML = "";
  pastUl.innerHTML = "";

  const upcoming = EVENTS.filter((e) => e.timeScope !== "past").sort(alleventsSortByDate("asc"));
  const past = EVENTS.filter((e) => e.timeScope === "past").sort(alleventsSortByDate("desc"));

  await Promise.all([
    ...upcoming.map((ev) => appendHorizontalEventCard(upcomingUl, ev.id)),
    ...past.map((ev) => appendHorizontalEventCard(pastUl, ev.id)),
  ]);

  const upcomingEmpty = root.querySelector('[data-allevents-empty="upcoming"]');
  const pastEmpty = root.querySelector('[data-allevents-empty="past"]');
  if (upcomingEmpty) upcomingEmpty.hidden = upcoming.length > 0;
  if (pastEmpty) pastEmpty.hidden = past.length > 0;
}

/********************************************************************************/

// Entry for All Events when panels need client-side hydration.
// @returns {Promise<void>}
async function initAlleventsList() {
  const root = document.querySelector("[data-allevents-list]");
  if (!root) return;
  await renderAlleventsLists(root);
}

// *************************************************************************************************
// My Events list
// *************************************************************************************************

// True when the viewer created or co-owns the event (matches list card role semantics).
// @param {typeof EVENTS[number]} e
// @returns {boolean}
function isMyEventsEvent(e) {
  const id = String(e?.id || "").trim();
  if (!id) return false;
  const creator = String(e?.creator || "")
    .trim()
    .toLowerCase();
  if (creator === "you" || creator === "me") return true;
  const key = normalizeMyEventsRoleKey(e);
  return key === "owner" || key === "co owner" || key === "coowner";
}

/********************************************************************************/

// Same date sort helper as All Events, scoped to the mine-only subset.
// @param {"asc" | "desc"} direction
// @returns {(a: typeof EVENTS[number], b: typeof EVENTS[number]) => number}
function myeventsSortByDate(direction) {
  const factor = direction === "desc" ? -1 : 1;
  return (a, b) => {
    const aMs = eventDateTimeMs(a);
    const bMs = eventDateTimeMs(b);
    if (aMs == null && bMs == null) return 0;
    if (aMs == null) return 1;
    if (bMs == null) return -1;
    return (aMs - bMs) * factor;
  };
}

/********************************************************************************/

// Fetches horizontal cards for viewer-owned events into upcoming/past panels.
// @param {HTMLElement} root
// @param {typeof EVENTS} mine
// @returns {Promise<void>}
async function renderMyEventsLists(root, mine) {
  const upcomingUl = root.querySelector(
    '[data-myevents-panel="upcoming"] [data-myevents-list-items="upcoming"]'
  );
  const pastUl = root.querySelector('[data-myevents-panel="past"] [data-myevents-list-items="past"]');
  if (!upcomingUl || !pastUl) return;

  upcomingUl.innerHTML = "";
  pastUl.innerHTML = "";

  const upcoming = mine.filter((e) => e.timeScope !== "past").sort(myeventsSortByDate("asc"));
  const past = mine.filter((e) => e.timeScope === "past").sort(myeventsSortByDate("desc"));

  await Promise.all([
    ...upcoming.map((ev) =>
      appendHorizontalEventCard(upcomingUl, ev.id, { fromMyEvents: true })
    ),
    ...past.map((ev) => appendHorizontalEventCard(pastUl, ev.id, { fromMyEvents: true })),
  ]);

  const upcomingEmpty = root.querySelector('[data-myevents-empty="upcoming"]');
  const pastEmpty = root.querySelector('[data-myevents-empty="past"]');
  if (upcomingEmpty) upcomingEmpty.hidden = upcoming.length > 0;
  if (pastEmpty) pastEmpty.hidden = past.length > 0;
}

/********************************************************************************/

// Entry for My Events: filters `EVENTS` then hydrates both panels.
// @returns {Promise<void>}
async function initMyEventsPage() {
  const root = document.querySelector("[data-myevents-list]");
  if (!root) return;

  const mine = EVENTS.filter(isMyEventsEvent);
  await renderMyEventsLists(root, mine);
}

/********************************************************************************/

document.addEventListener("DOMContentLoaded", () => {
  bootstrapPortalEvents();

  if (document.querySelector("[data-allevents-list]")) {
    initAlleventsList();
  }

  if (document.querySelector("[data-myevents-list]")) {
    initMyEventsPage();
  }
});

/********************************************************************************/
