// *************************************************************************************************
// eventdetails-chat.js — Tap a message to reveal delete; posts to DeleteChatMessage on confirm
// *************************************************************************************************

// - Deletable chat rows expose a trash control only while selected; selection clears on outside click.
// - Delete POST is handled by markup/forms; this module wires selection UX only.

// *************************************************************************************************
// Chat delete selection
// *************************************************************************************************

// Wires click/keyboard selection on deletable chat rows so the delete control appears once per row.
// @returns {void}
function bindEventDetailsChatDelete() {
  const chatList = document.getElementById("event-details-chat-list");
  if (!chatList) return;

  const deletableItems = /** @type {HTMLElement[]} */ (
    [...chatList.querySelectorAll(".event-details-chat-item[data-can-delete]")]
  );
  if (!deletableItems.length) return;

  // @param {HTMLElement | null} item
  // @returns {void}
  function setSelectedChatItem(item) {
    deletableItems.forEach((row) => {
      const selected = row === item;
      row.classList.toggle("event-details-chat-item--selected", selected);
      row.setAttribute("aria-expanded", selected ? "true" : "false");
    });
  }

  /********************************************************************************/

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

/********************************************************************************/
