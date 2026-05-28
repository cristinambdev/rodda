// *************************************************************************************************
// home-index.js — Home page (`Index.cshtml`)
// *************************************************************************************************

// - My To-Dos: click `.todo-task-line` rows to cross/uncross; persists via item/task `toggle-complete` POSTs.
// - Upcoming Events: uses `EVENTS` from `events-portal.js` into `[data-home-upcoming]` with range filtering.

// *************************************************************************************************
// Upcoming events segment (week / month / year)
// *************************************************************************************************

// Wires the segmented control and fetches vertical cards for events whose datetime falls in the active range.
// @returns {void}
function initHomeUpcomingSegment() {
  const block = document.querySelector("[data-home-upcoming]");
  if (!block || typeof EVENTS === "undefined") return;

  const buttons = block.querySelectorAll("[data-event-range]");
  const empty = block.querySelector("[data-event-empty]");
  const eventsList = block.querySelector("[data-events-list]");

  if (!buttons.length || !empty || !eventsList) return;

  // Re-renders the list and syncs pill `active` / `aria-pressed` state for the chosen range key.
  // @param {string} range
  // @returns {Promise<void>}
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

// *************************************************************************************************
// My To-Dos — cross / uncross checklist lines (server-rendered in `Index.cshtml`)
// *************************************************************************************************

// Wires click toggles on `.todo-task-line` and syncs card badges; POSTs to item/task toggle-complete endpoints.
// @returns {void}
function initHomeTodoTaskLines() {
  const root = document.querySelector("[data-todos-list]");
  if (!root) return;

  // Updates the `N/M done` badge on an expandable todo card after a line toggle.
  // @param {HTMLDetailsElement} card
  // @returns {void}
  function updateTodoCardBadge(card) {
    const badge = card.querySelector(".todo-task-card-badge");
    const lines = card.querySelectorAll(".todo-task-lines .todo-task-line");
    const done = card.querySelectorAll(".todo-task-lines .todo-task-line.done").length;
    if (badge && lines.length) {
      badge.textContent = `${done}/${lines.length} done`;
    }
  }

  // Applies or removes the visual done state on one checklist row.
  // @param {HTMLLIElement} line
  // @param {boolean} nowDone
  // @returns {void}
  function setTodoLineDone(line, nowDone) {
    line.classList.toggle("done", nowDone);
    const icon = line.querySelector(".todo-task-line-check i");
    if (icon) {
      icon.className = nowDone ? "fa-solid fa-circle-check" : "fa-regular fa-circle";
    }
  }

  // Persists completion for a bring item or guest task row; reverts UI on failure.
  // @param {HTMLLIElement} line
  // @param {boolean} targetDone
  // @returns {Promise<void>}
  async function persistTodoLineToggle(line, targetDone) {
    const card = line.closest("details.todo-task-card");
    const eventId = card?.getAttribute("data-event-id") || "";
    const kind = line.getAttribute("data-todo-kind");
    const itemId = line.getAttribute("data-item-id");
    const taskId = line.getAttribute("data-task-id");
    const resourceId = kind === "bring" ? itemId : kind === "task" ? taskId : "";
    const segment = kind === "bring" ? "items" : kind === "task" ? "tasks" : "";

    if (!eventId || !resourceId || !segment) return;

    const token =
      typeof window.PortalUi !== "undefined" && window.PortalUi.getAntiForgeryToken
        ? window.PortalUi.getAntiForgeryToken()
        : null;
    if (!token) {
      window.alert("Could not save. Please refresh and try again.");
      setTodoLineDone(line, !targetDone);
      if (card) updateTodoCardBadge(card);
      return;
    }

    const previousDone = !targetDone;
    setTodoLineDone(line, targetDone);
    if (card) updateTodoCardBadge(card);

    try {
      const response = await fetch(
        `/events/${encodeURIComponent(eventId)}/${segment}/${encodeURIComponent(resourceId)}/toggle-complete`,
        {
          method: "POST",
          headers: {
            Accept: "application/json",
            RequestVerificationToken: token,
            "X-XSRF-TOKEN": token,
          },
        }
      );

      if (!response.ok) {
        const payload = await response.json().catch(() => ({}));
        throw new Error(payload.error || "Could not update this to-do.");
      }

      const payload = await response.json().catch(() => ({}));
      const done = payload.done === true;
      setTodoLineDone(line, done);
      if (card) updateTodoCardBadge(card);
    } catch (error) {
      console.error("Home todo toggle failed:", error);
      setTodoLineDone(line, previousDone);
      if (card) updateTodoCardBadge(card);
      window.alert(error instanceof Error ? error.message : "Could not update this to-do.");
    }
  }

  root.addEventListener("click", (e) => {
    const line = e.target.closest(".todo-task-line");
    if (!line || !root.contains(line)) return;
    if (line.dataset.todoBusy === "1") return;

    e.preventDefault();
    const targetDone = !line.classList.contains("done");
    line.dataset.todoBusy = "1";
    persistTodoLineToggle(line, targetDone).finally(() => {
      delete line.dataset.todoBusy;
    });
  });

  root.querySelectorAll("details.todo-task-card").forEach(updateTodoCardBadge);
}

/********************************************************************************/

document.addEventListener("DOMContentLoaded", () => {
  initHomeTodoTaskLines();

  if (document.querySelector("[data-home-upcoming]")) {
    initHomeUpcomingSegment();
  }
});

/********************************************************************************/
