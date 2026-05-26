// *************************************************************************************************
// eventdetails-modals.js — Event details page actions (share, delete, hero menu)
// *************************************************************************************************

// - Hero ⋮ menu, share link copy/revoke, and delete-for-everyone flow on `#event-details-page`.
// - Modal open/close is delegated to `site.js` (`PortalUi`); this file binds page-specific actions only.

// *************************************************************************************************
// Page config
// *************************************************************************************************

// Parses the event id from the URL path when `data-event-id` is absent on the page root.
// @returns {string}
function getEventIdFromPath() {
  const pathSegments = window.location.pathname.split("/");
  return pathSegments[pathSegments.indexOf("events") + 1] || "";
}

/********************************************************************************/

// Reads dataset + DOM fallbacks so menu actions share one source for id, title, and permissions.
// @returns {{ eventId: string, eventTitle: string, canManage: boolean, listReturnUrl: string }}
function getEventDetailsPageConfig() {
  const page = document.getElementById("event-details-page");
  const params = new URLSearchParams(window.location.search);
  const fromMyEvents = params.get("from") === "myevents";
  return {
    eventId: page?.dataset.eventId || getEventIdFromPath(),
    eventTitle:
      page?.dataset.eventTitle ||
      document.getElementById("eventitem-title")?.textContent?.trim() ||
      "this event",
    canManage: page?.dataset.canManage === "true",
    listReturnUrl: fromMyEvents ? "/events/my-events" : "/events",
  };
}

/********************************************************************************/

// *************************************************************************************************
// List removal + share link
// *************************************************************************************************

// POSTs remove-from-list with anti-forgery token via `PortalUi.submitPostForm`.
// @param {string} eventId
// @param {string} returnUrl
// @returns {void}
function submitRemoveFromMyList(eventId, returnUrl) {
  if (!eventId || !window.PortalUi.getAntiForgeryToken()) {
    window.alert("Could not remove the event from your list.");
    return;
  }

  const query = returnUrl ? `?returnUrl=${encodeURIComponent(returnUrl)}` : "";
  window.PortalUi.submitPostForm(
    `/events/${encodeURIComponent(eventId)}/remove-from-list${query}`
  );
}

/********************************************************************************/
// Surfaces copy/revoke feedback in the share modal status line.
// @param {string} message
// @param {{ showCheck?: boolean }} [options]
// @returns {void}
function setShareStatus(message, options = {}) {
  const { showCheck = false } = options;
  const statusEl = document.getElementById("event-details-share-status");
  if (!statusEl) return;
  statusEl.hidden = false;

  const iconEl = statusEl.querySelector(".event-details-share-dialogue-status-icon");
  if (iconEl) {
    iconEl.hidden = !showCheck;
  }

  const textEl = statusEl.querySelector(".event-details-share-dialogue-status-text");
  if (textEl) {
    textEl.textContent = message;
    return;
  }
  statusEl.textContent = message;
}

/********************************************************************************/
// Fetches the active share URL and copies it to the clipboard.
// @returns {Promise<void>}
async function copyShareLink() {
  const { eventId } = getEventDetailsPageConfig();
  if (!eventId) return;

  try {
    const response = await fetch(`/events/${encodeURIComponent(eventId)}/share-link`, {
      headers: { Accept: "application/json" },
    });
    if (!response.ok) throw new Error("Could not load share link.");

    const data = await response.json();
    if (!data.url) throw new Error("Share link was empty.");

    await navigator.clipboard.writeText(data.url);
    setShareStatus("Link copied. Guests can open the event with this link.", {
      showCheck: true,
    });
  } catch (error) {
    console.error("Share link copy failed:", error);
    setShareStatus("Could not copy the share link.");
  }
}

/********************************************************************************/
// Revokes the active share token so a fresh link must be generated.
// @returns {Promise<void>}
async function revokeShareLink() {
  const { eventId } = getEventDetailsPageConfig();
  if (!eventId) return;

  const token = window.PortalUi.getAntiForgeryToken();
  try {
    const response = await fetch(`/events/${encodeURIComponent(eventId)}/share-link/revoke`, {
      method: "POST",
      headers: {
        Accept: "application/json",
        RequestVerificationToken: token,
        "X-XSRF-TOKEN": token,
      },
    });
    if (!response.ok) throw new Error("Could not revoke share link.");
    setShareStatus("Share link revoked. Generate a new one with Copy share link.");
  } catch (error) {
    console.error("Share link revoke failed:", error);
    setShareStatus("Could not revoke the share link.");
  }
}

// *************************************************************************************************
// Modal / menu bindings
// *************************************************************************************************
// Share modal: copy / revoke (open/close handled by `site.js` + portal modals).
// @returns {void}
function bindEventDetailsShareModal() {
  const shareModal = document.getElementById("event-details-share-dialog");
  if (!shareModal || shareModal.dataset.shareBound === "1") return;

  shareModal.dataset.shareBound = "1";
  shareModal.addEventListener("click", (event) => {
    const actionBtn = event.target.closest("[data-event-details-share-action]");
    if (!actionBtn) return;

    const action = actionBtn.getAttribute("data-event-details-share-action");
    if (action === "link") {
      copyShareLink();
      return;
    }
    if (action === "revoke") {
      revokeShareLink();
    }
  });
}

/********************************************************************************/
// Hero context menu actions (toggle handled by `site.js` `initAnchoredMenus`).
// @returns {void}
function bindEventDetailsContextMenuActions() {
  const menu = document.getElementById("event-details-context-menu");
  if (!menu || menu.dataset.menuActionsBound === "1") return;

  menu.dataset.menuActionsBound = "1";

  // @returns {void}
  function closeHeroContextMenu() {
    menu.hidden = true;
    document
      .getElementById("eventitem-hero-options")
      ?.setAttribute("aria-expanded", "false");
  }

  menu.addEventListener("click", (event) => {
    const btn = event.target.closest("[data-event-details-menu-action]");
    if (!btn) return;

    event.preventDefault();
    closeHeroContextMenu();
    const action = btn.getAttribute("data-event-details-menu-action");
    const { eventId, eventTitle, canManage } = getEventDetailsPageConfig();

    if (action === "share") {
      window.PortalUi.openModal("#event-details-share-dialog");
      return;
    }

    if (action === "edit") {
      if (!canManage) {
        window.alert("You don't have permission to edit this event.");
        return;
      }
      if (eventId) {
        window.location.href = `/events/create?edit=${encodeURIComponent(eventId)}`;
      }
      return;
    }

    if (action === "remove-from-list") {
      submitRemoveFromMyList(eventId, getEventDetailsPageConfig().listReturnUrl);
      return;
    }

    if (action === "delete") {
      if (!canManage) {
        window.alert("You don't have permission to delete this event.");
        return;
      }
      const deleteNameEl = document.getElementById("event-details-delete-event-name");
      if (deleteNameEl) deleteNameEl.textContent = eventTitle;
      const everyoneBtn = document.querySelector(
        '[data-event-details-delete-action="everyone"]'
      );
      if (everyoneBtn instanceof HTMLButtonElement) {
        everyoneBtn.disabled = !canManage;
      }
      window.PortalUi.openModal("#event-details-delete-dialog");
    }
  });
}

/********************************************************************************/

// Delete-for-everyone POST (list link and close use markup + `site.js`).
// @returns {void}
function bindEventDetailsDeleteModal() {
  const deleteModal = document.getElementById("event-details-delete-dialog");
  if (!deleteModal || deleteModal.dataset.deleteBound === "1") return;

  deleteModal.dataset.deleteBound = "1";

  deleteModal
    .querySelector('[data-event-details-delete-action="list"]')
    ?.addEventListener("click", () => {
      const { eventId, listReturnUrl } = getEventDetailsPageConfig();
      if (!eventId) return;
      submitRemoveFromMyList(eventId, listReturnUrl);
    });

  deleteModal
    .querySelector('[data-event-details-delete-action="everyone"]')
    ?.addEventListener("click", () => {
      const { eventId, canManage } = getEventDetailsPageConfig();
      if (!canManage || !eventId) return;

      if (!window.PortalUi.getAntiForgeryToken()) {
        window.alert("Could not submit delete request.");
        return;
      }

      window.PortalUi.submitPostForm(`/events/${encodeURIComponent(eventId)}/delete`);
    });
}

/********************************************************************************/

// Updates shield button + optional role badge after a successful co-owner toggle.
// @param {HTMLButtonElement} trigger
// @param {boolean} isCoOwner
// @param {string} guestName
// @returns {void}
function applyGuestCoOwnerUi(trigger, isCoOwner, guestName) {
  const label = guestName || "this guest";
  trigger.classList.toggle("is-coowner", isCoOwner);
  trigger.setAttribute("data-is-coowner", isCoOwner ? "true" : "false");
  trigger.setAttribute("aria-pressed", isCoOwner ? "true" : "false");
  trigger.setAttribute(
    "aria-label",
    isCoOwner ? `Remove co-ownership from ${label}` : `Make ${label} co-owner`
  );
  trigger.title = isCoOwner ? "Remove co-owner" : "Make co-owner";

  const row = trigger.closest(".event-details-members-row");
  const nameEl = row?.querySelector(".event-details-members-name");
  if (!nameEl) return;

  let roleEl = nameEl.querySelector(".event-details-members-role");
  if (isCoOwner) {
    if (!roleEl) {
      roleEl = document.createElement("span");
      roleEl.className = "event-details-members-role";
      nameEl.appendChild(roleEl);
    }
    roleEl.textContent = "Co-owner";
    return;
  }

  if (roleEl?.textContent?.trim() === "Co-owner") {
    roleEl.remove();
  }
}

/********************************************************************************/

// Toggles co-owner on a roster guest via POST/DELETE; updates row UI without reload.
// @param {string} targetUserId
// @param {string} guestName
// @param {HTMLButtonElement} trigger
// @returns {Promise<void>}
async function toggleGuestCoOwner(targetUserId, guestName, trigger) {
  const { eventId, canManage } = getEventDetailsPageConfig();
  if (!canManage || !eventId || !targetUserId) return;

  const isCoOwner = trigger.getAttribute("data-is-coowner") === "true";
  const token = window.PortalUi.getAntiForgeryToken();
  if (!token) {
    window.alert("Could not update co-owner. Please refresh and try again.");
    return;
  }

  trigger.disabled = true;

  try {
    const response = await fetch(`/events/${encodeURIComponent(eventId)}/co-owners`, {
      method: isCoOwner ? "DELETE" : "POST",
      headers: {
        Accept: "application/json",
        "Content-Type": "application/json",
        RequestVerificationToken: token,
        "X-XSRF-TOKEN": token,
      },
      body: JSON.stringify({ targetUserId }),
    });

    if (!response.ok) {
      const payload = await response.json().catch(() => ({}));
      throw new Error(
        payload.error ||
          (isCoOwner ? "Could not remove co-owner." : "Could not add co-owner.")
      );
    }

    applyGuestCoOwnerUi(trigger, !isCoOwner, guestName);
  } catch (error) {
    console.error("Toggle co-owner failed:", error);
    window.alert(error instanceof Error ? error.message : "Could not update co-owner.");
  } finally {
    trigger.disabled = false;
  }
}

/********************************************************************************/

// Delegated clicks on shield icons in `#event-details-guests-modal` (host roster only).
// @returns {void}
function bindEventDetailsGuestsCoOwnerActions() {
  const guestsModal = document.getElementById("event-details-guests-modal");
  if (!guestsModal || guestsModal.dataset.coOwnerBound === "1") return;

  guestsModal.dataset.coOwnerBound = "1";
  guestsModal.addEventListener("click", (event) => {
    const btn = event.target.closest("[data-event-details-toggle-coowner]");
    if (!btn || !(btn instanceof HTMLButtonElement) || btn.disabled) return;

    event.preventDefault();
    const targetUserId = btn.getAttribute("data-user-id") || "";
    const guestName = btn.getAttribute("data-guest-name") || "";
    toggleGuestCoOwner(targetUserId, guestName, btn);
  });
}

/********************************************************************************/

// Entry point: wires share, hero menu, and delete modals when the details page is present.
// @returns {void}
function initEventDetailsActions() {
  if (!document.getElementById("event-details-page")) return;

  bindEventDetailsShareModal();
  bindEventDetailsContextMenuActions();
  bindEventDetailsDeleteModal();
  bindEventDetailsGuestsCoOwnerActions();
}

/********************************************************************************/

document.addEventListener("DOMContentLoaded", initEventDetailsActions);

/********************************************************************************/
