// *************************************************************************************************
// events-portal.js — Portal event lists (All Events / My Events)
// *************************************************************************************************
// Reads `#portal-events-data` from the server, hydrates `EVENTS`, and renders horizontal cards
// into `[data-allevents-list]` or `[data-myevents-list]`. Card chrome uses
// `#rodda-horizontal-event-card-template` from `_HorizontalEventCard.cshtml`.

let EVENTS = [];

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

/**
 * Maps MVC list DTO fields into the shape expected by card renderers.
 * @param {any} raw
 * @returns {any}
 */
function normalizePortalEvent(raw) {
  const title = String(raw?.title || "").trim();
  return {
    id: String(raw?.id || "").trim(),
    title,
    listTitle: String(raw?.listTitle || title).trim(),
    creator: String(raw?.creator || "").trim(),
    hero: String(raw?.hero || "").trim(),
    listDateTime: String(raw?.listDateTime || "").trim(),
    listLocation: String(raw?.listLocation || "").trim(),
    timeScope: raw?.timeScope === "past" ? "past" : "upcoming",
    myEventsRole: raw?.myEventsRole != null ? String(raw.myEventsRole) : "",
    homeAttendance: raw?.homeAttendance != null ? String(raw.homeAttendance) : "",
    eventDateIso: String(raw?.eventDateIso || "").trim(),
    eventTime24: String(raw?.eventTime24 || "").trim(),
    itemsTasksEnabled: raw?.itemsTasksEnabled === true,
    items: 0,
    tasks: 0,
  };
}

/********************************************************************************/

/**
 * @param {any} event
 * @returns {number | null}
 */
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

/**
 * Keeps upcoming/past panels aligned with real start dates after server render.
 * @returns {void}
 */
function syncEventTimeScopes() {
  const now = Date.now();
  EVENTS.forEach((ev) => {
    const ms = eventDateTimeMs(ev);
    if (ms == null) return;
    ev.timeScope = ms < now ? "past" : "upcoming";
  });
}

/********************************************************************************/

// ================================================================================================
// Shared card helpers
// ================================================================================================

/**
 * @param {any} event
 * @returns {string}
 */
function listTitle(event) {
  return event.listTitle || event.title;
}

/********************************************************************************/

/**
 * @param {string} eventId
 * @param {{ fromMyEvents?: boolean }} [opts]
 * @returns {string}
 */
function eventDetailsPageHref(eventId, opts) {
  const id = encodeURIComponent(String(eventId || ""));
  const base = `/events/${id}`;
  if (opts?.fromMyEvents) return `${base}?from=myevents`;
  return base;
}

/********************************************************************************/

/**
 * @param {any} event
 * @returns {boolean}
 */
function eventHasCoverImage(event) {
  return Boolean(String(event?.hero || "").trim());
}

/********************************************************************************/

/**
 * @param {any} event
 * @returns {boolean}
 */
function eventIsCreator(event) {
  const creator = String(event?.creator || "")
    .trim()
    .toLowerCase();
  return creator === "you" || creator === "me";
}

/********************************************************************************/

/**
 * @param {any} event
 * @returns {string}
 */
function normalizeMyEventsRoleKey(event) {
  return String(event?.myEventsRole || "")
    .trim()
    .toLowerCase()
    .replace(/[^a-z]+/g, " ")
    .trim();
}

/********************************************************************************/

/**
 * @param {any} event
 * @returns {boolean}
 */
function eventIsOrganizerOrCoOwner(event) {
  const key = normalizeMyEventsRoleKey(event);
  return key === "owner" || key === "co owner" || key === "coowner";
}

/********************************************************************************/

/**
 * @param {any} event
 * @returns {"Creator" | "Owner" | "Co-owner" | ""}
 */
function eventRoleBadgeLabel(event) {
  if (eventIsCreator(event)) return "Creator";
  if (!eventIsOrganizerOrCoOwner(event)) return "";
  const key = normalizeMyEventsRoleKey(event);
  return key === "owner" ? "Owner" : "Co-owner";
}

/********************************************************************************/

/**
 * @param {any} event
 * @returns {{ state: string, label: string }}
 */
function eventAttendanceFooterState(event) {
  const raw = event?.homeAttendance;
  if (raw == null) return { state: "untracked", label: "Attendance not tracked" };
  const text = String(raw).trim();
  if (!text) return { state: "untracked", label: "Attendance not tracked" };
  if (/no\s*one/i.test(text)) return { state: "none", label: "No one has joined yet" };
  const match = text.match(/(\d+)/);
  const count = match ? Number(match[1]) : NaN;
  if (Number.isFinite(count) && count === 0) {
    return { state: "none", label: "No one has joined yet" };
  }
  return { state: "some", label: text };
}

/********************************************************************************/

/**
 * @param {HTMLElement | null} el
 * @param {any} event
 * @returns {void}
 */
function applyEventCardAttendanceRow(el, event) {
  if (!(el instanceof HTMLElement)) return;
  const { state, label } = eventAttendanceFooterState(event);
  const icon = state === "untracked" ? "fa-solid fa-circle-info" : "fa-solid fa-users";
  el.innerHTML = `<i class="${icon}" aria-hidden="true"></i>\n                        ${label}`;
  el.classList.toggle("event-card-attendance--muted", state !== "some");
  el.hidden = false;
}

/********************************************************************************/

/**
 * @returns {HTMLElement}
 */
function cloneHorizontalEventCardFromTemplate() {
  const tpl = document.getElementById("rodda-horizontal-event-card-template");
  const first = tpl instanceof HTMLTemplateElement ? tpl.content.firstElementChild : null;
  if (!(first instanceof HTMLElement)) {
    const fb = document.createElement("article");
    fb.className = "card event-card horizontal";
    fb.textContent = "Event card template missing";
    return fb;
  }
  return /** @type {HTMLElement} */ (first.cloneNode(true));
}

/********************************************************************************/

/**
 * @param {any} event
 * @param {{ past?: boolean, detailFromMyEvents?: boolean }} [options]
 * @returns {HTMLElement}
 */
function createHorizontalEventCardElement(event, options) {
  const ev = { ...event };
  const past = Boolean(options?.past);
  const fromMyEvents = Boolean(options?.detailFromMyEvents);
  const name = listTitle(ev);
  const article = cloneHorizontalEventCardFromTemplate();
  article.dataset.eventId = String(ev.id ?? "");
  if (past) article.classList.add("event-card--past");

  const href = eventDetailsPageHref(ev.id, { fromMyEvents });
  const hasCover = eventHasCoverImage(ev);
  const roleBadgeLabel = eventRoleBadgeLabel(ev);

  /**
   * @param {Element | null} node
   * @param {boolean} on
   */
  function setHidden(node, on) {
    if (!(node instanceof HTMLElement)) return;
    node.hidden = on;
    if (on) node.setAttribute("hidden", "");
    else node.removeAttribute("hidden");
  }

  const link = article.querySelector("[data-event-card-link]");
  if (link instanceof HTMLAnchorElement) {
    link.href = href;
    link.setAttribute("aria-label", `View ${name}`);
    link.classList.toggle("event-card-link-overlay--stack-only", !hasCover);
  }

  const thumb = article.querySelector("[data-event-card-thumb]");
  const img = article.querySelector("[data-event-card-image]");
  const thumbLink = article.querySelector("[data-event-card-thumb-link]");

  if (hasCover) {
    article.classList.remove("event-card--no-cover");
    if (thumb instanceof HTMLElement) thumb.classList.remove("event-thumb--placeholder");
    if (img instanceof HTMLImageElement) {
      img.src = ev.hero;
      img.alt = "";
      setHidden(img, false);
    }
    setHidden(thumbLink, true);
  } else {
    article.classList.add("event-card--no-cover");
    if (thumb instanceof HTMLElement) thumb.classList.add("event-thumb--placeholder");
    if (img instanceof HTMLImageElement) {
      img.removeAttribute("src");
      setHidden(img, true);
    }
    if (thumbLink instanceof HTMLAnchorElement) {
      thumbLink.href = href;
      thumbLink.setAttribute("aria-label", `View ${name}`);
      setHidden(thumbLink, false);
    }
  }

  const roleEl = article.querySelector("[data-event-card-role-badge]");
  if (roleEl) {
    if (roleBadgeLabel) {
      roleEl.textContent = roleBadgeLabel;
      setHidden(roleEl, false);
    } else {
      roleEl.textContent = "";
      setHidden(roleEl, true);
    }
  }

  const statusStack = article.querySelector("[data-event-card-status-stack]");
  if (statusStack instanceof HTMLElement) setHidden(statusStack, true);

  const dt = article.querySelector("[data-event-card-datetime]");
  if (dt) dt.textContent = ev.listDateTime || "";

  const titleEl = article.querySelector("[data-event-card-title]");
  if (titleEl) titleEl.textContent = name;

  const loc = article.querySelector("[data-event-card-location]");
  if (loc) {
    loc.innerHTML = `<i class="fa-solid fa-location-dot" aria-hidden="true"></i>\n                        ${ev.listLocation || ""}`;
  }

  applyEventCardAttendanceRow(article.querySelector("[data-event-card-attendance]"), ev);

  return article;
}

/********************************************************************************/

// ================================================================================================
// All Events list
// ================================================================================================

/**
 * @param {"asc" | "desc"} direction
 * @returns {(a: any, b: any) => number}
 */
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

/**
 * @param {HTMLElement} root
 * @returns {void}
 */
function renderAlleventsLists(root) {
  const upcomingUl = root.querySelector(
    '[data-allevents-panel="upcoming"] [data-allevents-list-items="upcoming"]'
  );
  const pastUl = root.querySelector('[data-allevents-panel="past"] [data-allevents-list-items="past"]');
  if (!upcomingUl || !pastUl) return;

  upcomingUl.innerHTML = "";
  pastUl.innerHTML = "";

  const upcoming = EVENTS.filter((e) => e.timeScope !== "past").sort(alleventsSortByDate("asc"));
  const past = EVENTS.filter((e) => e.timeScope === "past").sort(alleventsSortByDate("desc"));

  upcoming.forEach((ev) => {
    const li = document.createElement("li");
    li.className = "events-list-item";
    li.appendChild(createHorizontalEventCardElement(ev));
    upcomingUl.appendChild(li);
  });

  past.forEach((ev) => {
    const li = document.createElement("li");
    li.className = "events-list-item";
    li.appendChild(createHorizontalEventCardElement(ev, { past: true }));
    pastUl.appendChild(li);
  });

  const upcomingEmpty = root.querySelector('[data-allevents-empty="upcoming"]');
  const pastEmpty = root.querySelector('[data-allevents-empty="past"]');
  if (upcomingEmpty) upcomingEmpty.hidden = upcoming.length > 0;
  if (pastEmpty) pastEmpty.hidden = past.length > 0;
}

/********************************************************************************/

/**
 * @param {HTMLElement} root
 * @returns {void}
 */
function initAlleventsSegment(root) {
  const buttons = root.querySelectorAll("[data-allevents-range]");
  const upcoming = root.querySelector('[data-allevents-panel="upcoming"]');
  const past = root.querySelector('[data-allevents-panel="past"]');
  if (!buttons.length) return;

  /**
   * @param {string} range
   */
  function applyRange(range) {
    buttons.forEach((btn) => {
      const on = btn.getAttribute("data-allevents-range") === range;
      btn.classList.toggle("active", on);
      btn.setAttribute("aria-pressed", on ? "true" : "false");
    });
    if (upcoming) upcoming.hidden = range !== "upcoming";
    if (past) past.hidden = range !== "past";
  }

  buttons.forEach((btn) => {
    btn.addEventListener("click", () => {
      const range = btn.getAttribute("data-allevents-range");
      if (range === "upcoming" || range === "past") applyRange(range);
    });
  });

  const active = root.querySelector(".segmented-control-option.active[data-allevents-range]");
  applyRange(active?.getAttribute("data-allevents-range") || "upcoming");
}

/********************************************************************************/

/**
 * @returns {void}
 */
function initAlleventsList() {
  const root = document.querySelector("[data-allevents-list]");
  if (!root) return;

  renderAlleventsLists(root);
  initAlleventsSegment(root);
}

/********************************************************************************/

// ================================================================================================
// My Events list
// ================================================================================================

/**
 * @param {any} e
 * @returns {boolean}
 */
function isMyEventsEvent(e) {
  const creator = String(e?.creator || "")
    .trim()
    .toLowerCase();
  if (creator === "you" || creator === "me") return true;
  const key = normalizeMyEventsRoleKey(e);
  return key === "owner" || key === "co owner" || key === "coowner";
}

/********************************************************************************/

/**
 * @param {"asc" | "desc"} direction
 * @returns {(a: any, b: any) => number}
 */
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

/**
 * @param {HTMLElement} root
 * @param {any[]} mine
 * @returns {void}
 */
function renderMyEventsLists(root, mine) {
  const upcomingUl = root.querySelector(
    '[data-myevents-panel="upcoming"] [data-myevents-list-items="upcoming"]'
  );
  const pastUl = root.querySelector('[data-myevents-panel="past"] [data-myevents-list-items="past"]');
  if (!upcomingUl || !pastUl) return;

  upcomingUl.innerHTML = "";
  pastUl.innerHTML = "";

  const upcoming = mine.filter((e) => e.timeScope !== "past").sort(myeventsSortByDate("asc"));
  const past = mine.filter((e) => e.timeScope === "past").sort(myeventsSortByDate("desc"));

  upcoming.forEach((ev) => {
    const li = document.createElement("li");
    li.className = "events-list-item";
    li.appendChild(
      createHorizontalEventCardElement(ev, { past: false, detailFromMyEvents: true })
    );
    upcomingUl.appendChild(li);
  });

  past.forEach((ev) => {
    const li = document.createElement("li");
    li.className = "events-list-item";
    li.appendChild(
      createHorizontalEventCardElement(ev, { past: true, detailFromMyEvents: true })
    );
    pastUl.appendChild(li);
  });

  const upcomingEmpty = root.querySelector('[data-myevents-empty="upcoming"]');
  const pastEmpty = root.querySelector('[data-myevents-empty="past"]');
  if (upcomingEmpty) upcomingEmpty.hidden = upcoming.length > 0;
  if (pastEmpty) pastEmpty.hidden = past.length > 0;
}

/********************************************************************************/

/**
 * @param {HTMLElement} root
 * @returns {void}
 */
function initMyEventsSegment(root) {
  const buttons = root.querySelectorAll("[data-myevents-range]");
  const upcoming = root.querySelector('[data-myevents-panel="upcoming"]');
  const past = root.querySelector('[data-myevents-panel="past"]');
  if (!buttons.length) return;

  /**
   * @param {string} range
   */
  function applyRange(range) {
    buttons.forEach((btn) => {
      const on = btn.getAttribute("data-myevents-range") === range;
      btn.classList.toggle("active", on);
      btn.setAttribute("aria-pressed", on ? "true" : "false");
    });
    if (upcoming) upcoming.hidden = range !== "upcoming";
    if (past) past.hidden = range !== "past";
  }

  buttons.forEach((btn) => {
    btn.addEventListener("click", () => {
      const range = btn.getAttribute("data-myevents-range");
      if (range === "upcoming" || range === "past") applyRange(range);
    });
  });

  const active = root.querySelector(".segmented-control-option.active[data-myevents-range]");
  applyRange(active?.getAttribute("data-myevents-range") || "upcoming");
}

/********************************************************************************/

/**
 * @returns {void}
 */
function initMyEventsPage() {
  const root = document.querySelector("[data-myevents-list]");
  if (!root) return;

  const mine = EVENTS.filter(isMyEventsEvent);
  renderMyEventsLists(root, mine);
  initMyEventsSegment(root);
}

/********************************************************************************/

// ================================================================================================
// Page bootstrap
// ================================================================================================

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
