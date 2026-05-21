// *************************************************************************************************
// eventdetails-chat.js — Tap a message to reveal delete; posts to DeleteChatMessage on confirm
// *************************************************************************************************

/**
 * Wires click/keyboard selection on deletable chat rows so the delete control appears once.
 */
function bindEventDetailsChatDelete() {
  const chatList = document.getElementById("event-details-chat-list");
  if (!chatList) return;

  const deletableItems = /** @type {HTMLElement[]} */ (
    [...chatList.querySelectorAll(".event-details-chat-item[data-can-delete]")]
  );
  if (!deletableItems.length) return;

  // **********************************************************************************************
  // Selection state
  // **********************************************************************************************

  /**
   * @param {HTMLElement | null} item
   */
  function setSelectedChatItem(item) {
    deletableItems.forEach((row) => {
      const selected = row === item;
      row.classList.toggle("event-details-chat-item--selected", selected);
      row.setAttribute("aria-expanded", selected ? "true" : "false");
    });
  }

  // **********************************************************************************************
  // Event handlers
  // **********************************************************************************************

  deletableItems.forEach((item) => {
    item.addEventListener("click", (e) => {
      if (e.target.closest(".event-details-chat-delete")) return;
      const alreadySelected = item.classList.contains("event-details-chat-item--selected");
      setSelectedChatItem(alreadySelected ? null : item);
    });

    item.addEventListener("keydown", (e) => {
      if (e.key !== "Enter" && e.key !== " ") return;
      e.preventDefault();
      const alreadySelected = item.classList.contains("event-details-chat-item--selected");
      setSelectedChatItem(alreadySelected ? null : item);
    });
  });

  document.addEventListener("click", (e) => {
    if (e.target.closest(".event-details-chat-item[data-can-delete]")) return;
    if (e.target.closest(".event-details-chat-delete")) return;
    setSelectedChatItem(null);
  });
}

/********************************************************************************/

document.addEventListener("DOMContentLoaded", () => {
  if (!document.getElementById("event-details-page")) return;
  bindEventDetailsChatDelete();
});
