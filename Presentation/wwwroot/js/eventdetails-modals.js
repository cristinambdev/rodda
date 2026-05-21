// *************************************************************************************************
// eventdetails-modals.js — Event details page actions (share, delete, hero menu)
// *************************************************************************************************

/**
 * @returns {string}
 */
function getEventIdFromPath() {
  const pathSegments = window.location.pathname.split("/");
  return pathSegments[pathSegments.indexOf("events") + 1] || "";
}

/**
 * @returns {{ eventId: string, eventTitle: string, canManage: boolean }}
 */
function getEventDetailsPageConfig() {
  const page = document.getElementById("event-details-page");
  return {
    eventId: page?.dataset.eventId || getEventIdFromPath(),
    eventTitle:
      page?.dataset.eventTitle ||
      document.getElementById("eventitem-title")?.textContent?.trim() ||
      "this event",
    canManage: page?.dataset.canManage === "true",
  };
}

/**
 * @param {string} message
 * @returns {void}
 */
function setShareStatus(message) {
  const statusEl = document.getElementById("event-details-share-status");
  if (!statusEl) return;
  statusEl.hidden = false;
  statusEl.textContent = message;
}

/**
 * @returns {Promise<void>}
 */
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
    setShareStatus("Link copied. Anyone with this link can join while it stays active.");
  } catch (error) {
    console.error("Share link copy failed:", error);
    setShareStatus("Could not copy the share link.");
  }
}

/**
 * @returns {Promise<void>}
 */
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

/********************************************************************************/

/**
 * Share modal: copy / revoke (open/close handled by `site.js` + portal modals).
 * @returns {void}
 */
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

/**
 * Hero context menu actions (toggle handled by `site.js` `initAnchoredMenus`).
 * @returns {void}
 */
function bindEventDetailsContextMenuActions() {
  const menu = document.getElementById("event-details-context-menu");
  if (!menu || menu.dataset.menuActionsBound === "1") return;

  menu.dataset.menuActionsBound = "1";

  /**
   * @returns {void}
   */
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

/**
 * Delete-for-everyone POST (list link and close use markup + `site.js`).
 * @returns {void}
 */
function bindEventDetailsDeleteModal() {
  const deleteModal = document.getElementById("event-details-delete-dialog");
  if (!deleteModal || deleteModal.dataset.deleteBound === "1") return;

  deleteModal.dataset.deleteBound = "1";

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

/**
 * @returns {void}
 */
function initEventDetailsActions() {
  if (!document.getElementById("event-details-page")) return;

  bindEventDetailsShareModal();
  bindEventDetailsContextMenuActions();
  bindEventDetailsDeleteModal();
}

document.addEventListener("DOMContentLoaded", initEventDetailsActions);
