// *************************************************************************************************
// event-items-tasks.js — Items/tasks modals on event details and create (`#eventitem-modals-root`)
// *************************************************************************************************
function isEventModalDraft() {
  return document.getElementById("eventitem-modals-root")?.dataset.eventModalDraft === "true";
}

// ***********************************************************************************************************
function isPersistableEventId(eventId) {
  return Boolean(eventId) && eventId !== "create";
}

// ***********************************************************************************************************
function getEventIdFromPath() {
  const pathSegments = window.location.pathname.split("/");
  return pathSegments[pathSegments.indexOf("events") + 1] || "";
}

// ***********************************************************************************************************
async function updateEventSettings() {
  if (isEventModalDraft()) return;

  const eventId = getEventIdFromPath();
  if (!isPersistableEventId(eventId)) {
    console.error("Could not resolve Event ID from the URL pathway configuration.");
    return;
  }

  const allowItemsCheckbox = document.getElementById("toggle-allow-items");
  const allowTasksCheckbox = document.getElementById("toggle-allow-tasks");
  if (!allowItemsCheckbox || !allowTasksCheckbox) return;

  const previousItems = allowItemsCheckbox.checked;
  const previousTasks = allowTasksCheckbox.checked;

  const payload = {
    Id: eventId,
    AllowGuestBringItems: allowItemsCheckbox.checked,
    AllowGuestTasks: allowTasksCheckbox.checked,
  };

  const token = window.PortalUi.getAntiForgeryToken();
  if (!token) {
    console.error("Anti-forgery token missing from the active document workspace.");
    return;
  }

  try {
    const response = await fetch(`/events/${encodeURIComponent(eventId)}/update-settings`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Accept: "application/json",
        RequestVerificationToken: token,
        "X-XSRF-TOKEN": token,
      },
      body: JSON.stringify(payload),
    });

    if (!response.ok) {
      const errorData = await response.json().catch(() => ({}));
      throw new Error(errorData.error || `HTTP error! status: ${response.status}`);
    }

    const data = await response.json();
    if (data.succeeded) {
      window.location.reload();
    }
  } catch (error) {
    console.error("Failed to persist event toggles state:", error);
    alert(`Failed to save settings: ${error.message}`);
    allowItemsCheckbox.checked = previousItems;
    allowTasksCheckbox.checked = previousTasks;
  }
}

/********************************************************************************/
function bindGuestPolicyToggles() {
  if (isEventModalDraft()) return;

  const allowItemsCheckbox = document.getElementById("toggle-allow-items");
  const allowTasksCheckbox = document.getElementById("toggle-allow-tasks");

  if (allowItemsCheckbox && !allowItemsCheckbox.dataset.guestPolicyBound) {
    allowItemsCheckbox.dataset.guestPolicyBound = "1";
    allowItemsCheckbox.addEventListener("change", updateEventSettings);
  }
  if (allowTasksCheckbox && !allowTasksCheckbox.dataset.guestPolicyBound) {
    allowTasksCheckbox.dataset.guestPolicyBound = "1";
    allowTasksCheckbox.addEventListener("change", updateEventSettings);
  }
}

/********************************************************************************/
function readDataAttr(value, fallback = "") {
  return value != null && String(value).length > 0 ? String(value) : fallback;
}

/********************************************************************************/
/**
 * Fills the add-item modal for edit; runs before the modal opens via `data-type="modal"`.
 */
function populateAddItemFormForEdit(trigger, addItemForm) {
  if (!addItemForm || addItemForm.dataset.draftOnly === "true") return false;

  const template = addItemForm.dataset.editUrlTemplate;
  const itemId = readDataAttr(trigger.dataset.itemId);
  if (!template || !itemId) return false;

  const row = trigger.closest("[data-item-id]");
  const title =
    readDataAttr(trigger.dataset.itemTitle) ||
    row?.querySelector(".eventitem-bring-row-title")?.textContent?.trim() ||
    "";
  const amount =
    readDataAttr(trigger.dataset.itemAmount) ||
    row?.querySelector(".eventitem-bring-row-meta span")?.textContent?.trim() ||
    "";

  addItemForm.action = template.replace("__ITEMID__", encodeURIComponent(itemId));

  const nameEl = document.getElementById("eventitem-add-item-name");
  const amountEl = document.getElementById("eventitem-add-item-amount");
  const peopleEl = document.getElementById("eventitem-add-item-people");
  const idEl = document.getElementById("eventitem-add-item-id");
  const titleEl = document.getElementById("eventitem-add-item-modal-title");

  if (nameEl) nameEl.value = title;
  if (amountEl) amountEl.value = amount;
  if (peopleEl) peopleEl.value = readDataAttr(trigger.dataset.itemPeople, "1");
  if (idEl) idEl.value = itemId;
  if (titleEl) titleEl.textContent = "Edit item";

  return true;
}

/********************************************************************************/
/**
 * Fills the add-task modal for edit; runs before the modal opens via `data-type="modal"`.
 */
function populateAddTaskFormForEdit(trigger, addTaskForm) {
  if (!addTaskForm || addTaskForm.dataset.draftOnly === "true") return false;

  const template = addTaskForm.dataset.editUrlTemplate;
  const taskId = readDataAttr(trigger.dataset.taskId);
  if (!template || !taskId) return false;

  const row = trigger.closest("[data-task-id]");
  const title =
    readDataAttr(trigger.dataset.taskTitle) ||
    row?.querySelector(".eventitem-bring-row-title")?.textContent?.trim() ||
    "";

  addTaskForm.action = template.replace("__TASKID__", encodeURIComponent(taskId));

  const nameEl = document.getElementById("eventitem-add-task-name");
  const timeEl = document.getElementById("eventitem-add-task-time");
  const locationEl = document.getElementById("eventitem-add-task-location");
  const peopleEl = document.getElementById("eventitem-add-task-people");
  const idEl = document.getElementById("eventitem-add-task-id");
  const titleEl = document.getElementById("eventitem-add-task-modal-title");

  if (nameEl) nameEl.value = title;
  if (timeEl) {
    timeEl.value =
      readDataAttr(trigger.dataset.taskTime) ||
      row?.querySelector(".eventitem-task-meta-line .fa-clock")?.parentElement?.textContent?.trim() ||
      "";
  }
  if (locationEl) {
    locationEl.value =
      readDataAttr(trigger.dataset.taskLocation) ||
      row?.querySelector(".eventitem-task-meta-line .fa-location-dot")?.parentElement?.textContent?.trim() ||
      "";
  }
  if (peopleEl) peopleEl.value = readDataAttr(trigger.dataset.taskPeople, "1");
  if (idEl) idEl.value = taskId;
  if (titleEl) titleEl.textContent = "Edit task";

  return true;
}

/********************************************************************************/
function initEventItemsTasksModals() {
  const modalsRoot = document.getElementById("eventitem-modals-root");
  if (!modalsRoot) return;

  const addItemForm = document.getElementById("eventitem-add-item-form");
  const addTaskForm = document.getElementById("eventitem-add-task-form");

  applyManageGates();
  bindGuestPolicyToggles();

  document.addEventListener("portal-ui:before-modal-open", (e) => {
    const trigger = e.detail?.trigger;
    if (!trigger) return;

    if (trigger.hasAttribute("data-edit-item")) {
      populateAddItemFormForEdit(trigger, addItemForm);
      return;
    }

    if (trigger.hasAttribute("data-edit-task")) {
      populateAddTaskFormForEdit(trigger, addTaskForm);
      return;
    }

    if (trigger.hasAttribute("data-reset-add-item")) {
      resetAddItemForm(addItemForm);
    }
    if (trigger.hasAttribute("data-reset-add-task")) {
      resetAddTaskForm(addTaskForm);
    }
  });

// ***********************************************************************************************************
  function applyManageGates() {
    if (modalsRoot.dataset.eventModalDraft === "true") return;

    const itemModal = document.getElementById("eventitems-modal");
    const taskModal = document.getElementById("eventtasks-modal");
    const canManageItems = itemModal?.dataset.canManage === "true";
    const canManageTasks = taskModal?.dataset.canManage === "true";
    const canAddItems = itemModal?.dataset.canAdd === "true";
    const canAddTasks = taskModal?.dataset.canAdd === "true";

    if (!canManageItems) {
      document.getElementById("eventitem-items-guest-policy-row")?.remove();
    }
    if (!canManageTasks) {
      document.getElementById("eventtasks-guest-policy-row")?.remove();
    }
    if (!canManageItems && !canAddItems) {
      document.getElementById("eventitems-modal-add")?.remove();
    }
    if (!canManageTasks && !canAddTasks) {
      document.getElementById("eventtasks-modal-add")?.remove();
    }
  }

// ***********************************************************************************************************
  function resetAddItemForm(form) {
    if (!form || form.dataset.draftOnly === "true") return;
    const createAction = form.getAttribute("action");
    if (createAction) form.action = createAction;
    const titleEl = document.getElementById("eventitem-add-item-modal-title");
    if (titleEl) titleEl.textContent = "Add item";
    const idEl = document.getElementById("eventitem-add-item-id");
    if (idEl) idEl.value = "";
    form.reset();
    const people = document.getElementById("eventitem-add-item-people");
    if (people) people.value = "1";
  }

// ***********************************************************************************************************
  function resetAddTaskForm(form) {
    if (!form || form.dataset.draftOnly === "true") return;
    const createAction = form.getAttribute("action");
    if (createAction) form.action = createAction;
    const titleEl = document.getElementById("eventitem-add-task-modal-title");
    if (titleEl) titleEl.textContent = "Add task";
    const idEl = document.getElementById("eventitem-add-task-id");
    if (idEl) idEl.value = "";
    form.reset();
    const people = document.getElementById("eventitem-add-task-people");
    if (people) people.value = "1";
  }
}

document.addEventListener("DOMContentLoaded", initEventItemsTasksModals);
