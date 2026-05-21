// *************************************************************************************************
// eventdetails-join.js — Join button + group-size dialog on event details
// *************************************************************************************************

function bindEventDetailsJoinDialog() {
  const openBtn = document.getElementById("eventitem-join-open-btn");
  const joinDialog = document.getElementById("event-details-join-dialog");
  const joinGroupInput = document.getElementById("event-details-join-group-size");
  const closeBtn = document.querySelector(".event-details-join-dialogue-close");

  if (!openBtn || !joinDialog) return;

/********************************************************************************/
  function openJoinDialog() {
    if (joinGroupInput) {
      joinGroupInput.value = "1";
      queueMicrotask(() => joinGroupInput.focus());
    }
    if (typeof joinDialog.showModal === "function") {
      joinDialog.showModal();
    }
  }

 /********************************************************************************/
  function closeJoinDialog() {
    if (typeof joinDialog.close === "function") {
      joinDialog.close();
    }
  }

  openBtn.addEventListener("click", openJoinDialog);
  closeBtn?.addEventListener("click", closeJoinDialog);
  joinDialog.addEventListener("click", (e) => {
    if (e.target === joinDialog) closeJoinDialog();
  });
  joinDialog.addEventListener("cancel", (e) => {
    e.preventDefault();
    closeJoinDialog();
  });
}

/********************************************************************************/

document.addEventListener("DOMContentLoaded", () => {
  if (!document.getElementById("event-details-page")) return;
  bindEventDetailsJoinDialog();
});
