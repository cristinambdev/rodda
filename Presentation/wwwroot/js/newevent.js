// *************************************************************************************************
// newevent.js — New Event form (BackOffice MVC `/events/create`)
// *************************************************************************************************

// Wires toolbar back navigation, cover preview, disclosure panels, and filled-state styling.
// Form submit is handled by the server (`EventsController.Add`); this module does not intercept POST.

// *************************************************************************************************
// Page bootstrap
// *************************************************************************************************

/**
 * Initializes the new-event page when `#edit-event-form` is present.
 * @returns {void}
 */
function initNewEventPage() {
  const form = document.getElementById("edit-event-form");
  if (!form) return;

  const back = document.getElementById("newevent-back");
  const coverInput = document.getElementById("cover-input");
  const coverLabelEmpty = document.getElementById("cover-label-empty");
  const coverPreview = document.getElementById("cover-preview");
  const coverImg = document.getElementById("cover-img");
  const coverDelete = document.getElementById("cover-delete");
  const description = document.getElementById("newevent-description");
  const expensesPanel = document.querySelector("[data-newevent-expenses-panel]");
  const expensesDisclosureRoot = document.getElementById("newevent-shared-expenses");
  const expensesDisclosureTrigger = document.getElementById("newevent-expenses-trigger");
  const itemsTasksEnabledInput = document.getElementById("newevent-items-tasks-enabled");
  const itemsTasksDisclosureRoot = document.getElementById("newevent-items-tasks-card");
  const itemsTasksDisclosureTrigger = document.getElementById("newevent-items-tasks-trigger");
  const itemsTasksSection = document.querySelector(".newevent-items-section");

  let expensesOpen = false;
  let itemsTasksOpen = false;

  const fillStateControls = Array.from(form.querySelectorAll("input, textarea, select")).filter((el) => {
    const type = el.tagName.toLowerCase() === "input" ? String(/** @type {HTMLInputElement} */ (el).type || "").toLowerCase() : "";
    return type !== "hidden" && type !== "checkbox" && type !== "radio" && type !== "file";
  });

  /**
   * Toggles filled-state classes used by `newevent.css` for populated fields.
   * @param {HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement} control
   * @returns {void}
   */
  function applyFilledState(control) {
    const isFilled = String(control.value || "").trim().length > 0;
    control.classList.toggle("newevent-field-filled", isFilled);
    const shell = control.closest("div.form-input-field");
    if (shell) shell.classList.toggle("newevent-field-shell-filled", isFilled);
  }

  /**
   * Recomputes filled styling after programmatic updates.
   * @returns {void}
   */
  function refreshFilledStates() {
    fillStateControls.forEach((control) => applyFilledState(control));
  }

  fillStateControls.forEach((control) => {
    control.addEventListener("input", () => applyFilledState(control));
    control.addEventListener("change", () => applyFilledState(control));
  });

  /**
   * @param {string} url
   * @returns {void}
   */
  function showCoverPreview(url) {
    if (coverImg) coverImg.src = url;
    coverLabelEmpty?.setAttribute("hidden", "");
    coverPreview?.removeAttribute("hidden");
  }

  /**
   * @returns {void}
   */
  function showCoverEmpty() {
    if (coverInput) coverInput.value = "";
    if (coverImg) coverImg.src = "";
    coverLabelEmpty?.removeAttribute("hidden");
    coverPreview?.setAttribute("hidden", "");
  }

  coverInput?.addEventListener("change", () => {
    const file = coverInput.files?.[0];
    if (file && file.type.startsWith("image/")) {
      showCoverPreview(URL.createObjectURL(file));
    }
  });

  coverDelete?.addEventListener("click", (e) => {
    e.preventDefault();
    e.stopPropagation();
    showCoverEmpty();
  });

  coverLabelEmpty?.addEventListener("keydown", (e) => {
    if (e.key === "Enter" || e.key === " ") {
      e.preventDefault();
      coverInput?.click();
    }
  });

  back?.addEventListener("click", () => {
    if (window.history.length > 1) {
      window.history.back();
      return;
    }
    window.location.href = "/events";
  });

  /**
   * @param {string} wrap
   * @returns {void}
   */
  function wrapSelection(textarea, wrap) {
    const start = textarea.selectionStart ?? 0;
    const end = textarea.selectionEnd ?? 0;
    const val = textarea.value;
    const selected = val.slice(start, end);
    const insertion = wrap + selected + wrap;
    textarea.value = val.slice(0, start) + insertion + val.slice(end);
    const pos = start + insertion.length;
    textarea.setSelectionRange(pos, pos);
    textarea.focus();
  }

  document.querySelectorAll(".newevent-description-toolbar [data-desc-cmd]").forEach((btn) => {
    btn.addEventListener("click", () => {
      if (!description) return;
      const cmd = btn.getAttribute("data-desc-cmd");
      if (cmd === "bold") wrapSelection(description, "**");
      else if (cmd === "italic") wrapSelection(description, "_");
      else if (cmd === "underline") wrapSelection(description, "__");
    });
  });

  /**
   * @returns {void}
   */
  function syncNewEventExpensesDisclosureUi() {
    expensesDisclosureTrigger?.setAttribute("aria-expanded", expensesOpen ? "true" : "false");
    expensesDisclosureRoot?.classList.toggle("newevent-disclosure-open", expensesOpen);
  }

  /********************************************************************************/

  /**
   * @returns {void}
   */
  function syncNewEventItemsTasksDisclosureUi() {
    itemsTasksDisclosureTrigger?.setAttribute("aria-expanded", itemsTasksOpen ? "true" : "false");
    itemsTasksDisclosureRoot?.classList.toggle("newevent-disclosure-open", itemsTasksOpen);
  }

  /********************************************************************************/

  /**
   * @param {boolean} enabled
   * @returns {void}
   */
  function setExpensesEnabled(enabled) {
    expensesOpen = enabled;
    if (expensesPanel) expensesPanel.hidden = !enabled;
    if (!enabled) {
      const methodEl = form.querySelector('[name="expensesMethod"]');
      const phoneEl = form.querySelector('[name="expensesPhone"]');
      const amountEl = form.querySelector('[name="expensesAmount"]');
      const nameEl = form.querySelector('[name="expensesName"]');
      if (methodEl) methodEl.value = "";
      if (phoneEl) phoneEl.value = "";
      if (amountEl) amountEl.value = "";
      if (nameEl) nameEl.value = "";
    }
    syncNewEventExpensesDisclosureUi();
  }

  setExpensesEnabled(false);
  expensesDisclosureTrigger?.addEventListener("click", () => {
    setExpensesEnabled(!expensesOpen);
  });

  /**
   * @param {boolean} enabled
   * @returns {void}
   */
  function setItemsTasksEnabled(enabled) {
    itemsTasksOpen = enabled;
    if (itemsTasksEnabledInput instanceof HTMLInputElement) {
      itemsTasksEnabledInput.value = enabled ? "true" : "false";
    }
    if (itemsTasksSection instanceof HTMLElement) {
      itemsTasksSection.hidden = !enabled;
    }
    document.querySelectorAll("[data-open-bring-items-modal], [data-open-eventtasks-modal]").forEach((el) => {
      if (!(el instanceof HTMLButtonElement)) return;
      el.disabled = !enabled;
      el.setAttribute("aria-disabled", enabled ? "false" : "true");
      el.setAttribute("tabindex", enabled ? "0" : "-1");
    });
    syncNewEventItemsTasksDisclosureUi();
  }

  setItemsTasksEnabled(false);
  itemsTasksDisclosureTrigger?.addEventListener("click", () => {
    setItemsTasksEnabled(!itemsTasksOpen);
  });

  refreshFilledStates();
  initNewEventDraftModals();
}

/**
 * Client-side items/tasks modals on `/events/create` (no event id yet).
 * @returns {void}
 */
function initNewEventDraftModals() {
  const addItemForm = document.getElementById("eventitem-add-item-form");
  if (!addItemForm || addItemForm.dataset.draftOnly !== "true") return;

  const itemRowsEl = document.getElementById("eventitems-modal-rows");
  const taskRowsEl = document.getElementById("eventtasks-modal-rows");
  const itemsCountEl = document.getElementById("eventitem-items-count");
  const tasksCountEl = document.getElementById("eventtasks-count");
  const itemsSubtitleEl = document.getElementById("eventitems-modal-subtitle");
  const tasksSubtitleEl = document.getElementById("eventtasks-modal-subtitle");
  const addItemSubtitleEl = document.getElementById("eventitem-add-item-modal-subtitle");
  const addTaskSubtitleEl = document.getElementById("eventitem-add-task-modal-subtitle");
  const allowItemsToggle = document.getElementById("toggle-allow-items");
  const allowTasksToggle = document.getElementById("toggle-allow-tasks");
  const allowItemsHidden = document.getElementById("newevent-allow-guest-items");
  const allowTasksHidden = document.getElementById("newevent-allow-guest-tasks");
  const itemListModal = document.getElementById("eventitems-modal");
  const taskListModal = document.getElementById("eventtasks-modal");
  const addItemModal = document.getElementById("eventitem-add-item-modal");
  const addTaskModal = document.getElementById("eventitem-add-task-modal");
  const addTaskForm = document.getElementById("eventitem-add-task-form");

  /** @type {{ bringItems: Array<{ title: string, amount: string, people: number }>, guestTasks: Array<{ title: string, location: string, time: string, people: number }> }} */
  const draft = { bringItems: [], guestTasks: [] };
  let editingItemIndex = -1;
  let editingTaskIndex = -1;

  const addItemModalTitle = document.getElementById("eventitem-add-item-modal-title");
  const addTaskModalTitle = document.getElementById("eventitem-add-task-modal-title");

  /**
   * @returns {string}
   */
  function draftEventTitle() {
    const titleInput = document.getElementById("newevent-title");
    return titleInput?.value?.trim() || "New event";
  }

  /**
   * @returns {void}
   */
  function syncDraftModalTitles() {
    const t = draftEventTitle().toUpperCase();
    if (itemsSubtitleEl) itemsSubtitleEl.textContent = t;
    if (tasksSubtitleEl) tasksSubtitleEl.textContent = t;
    if (addItemSubtitleEl) addItemSubtitleEl.textContent = t;
    if (addTaskSubtitleEl) addTaskSubtitleEl.textContent = t;
  }

  /**
   * @returns {void}
   */
  function resetDraftItemForm() {
    editingItemIndex = -1;
    addItemForm.reset();
    const peopleEl = document.getElementById("eventitem-add-item-people");
    if (peopleEl) peopleEl.value = "1";
    if (addItemModalTitle) addItemModalTitle.textContent = "Add item";
  }

  /**
   * @returns {void}
   */
  function resetDraftTaskForm() {
    editingTaskIndex = -1;
    if (!addTaskForm) return;
    addTaskForm.reset();
    const peopleEl = document.getElementById("eventitem-add-task-people");
    if (peopleEl) peopleEl.value = "1";
    if (addTaskModalTitle) addTaskModalTitle.textContent = "Add task";
  }

  /**
   * @param {number} index
   * @returns {void}
   */
  function openDraftItemForEdit(index) {
    const row = draft.bringItems[index];
    if (!row) return;

    editingItemIndex = index;
    document.getElementById("eventitem-add-item-name").value = row.title;
    document.getElementById("eventitem-add-item-amount").value = row.amount || "";
    document.getElementById("eventitem-add-item-people").value = String(row.people || 1);
    if (addItemModalTitle) addItemModalTitle.textContent = "Edit item";
    syncDraftModalTitles();
    window.PortalUi.setModalVisible(itemListModal, false);
    window.PortalUi.setModalVisible(addItemModal, true);
  }

  /**
   * @param {number} index
   * @returns {void}
   */
  function openDraftTaskForEdit(index) {
    const row = draft.guestTasks[index];
    if (!row) return;

    editingTaskIndex = index;
    document.getElementById("eventitem-add-task-name").value = row.title;
    document.getElementById("eventitem-add-task-location").value = row.location || "";
    document.getElementById("eventitem-add-task-time").value = row.time || "";
    document.getElementById("eventitem-add-task-people").value = String(row.people || 1);
    if (addTaskModalTitle) addTaskModalTitle.textContent = "Edit task";
    syncDraftModalTitles();
    window.PortalUi.setModalVisible(taskListModal, false);
    window.PortalUi.setModalVisible(addTaskModal, true);
  }

  /**
   * @param {HTMLElement | null} rowsEl
   * @param {Array<{ title: string, amount?: string, location?: string, time?: string, people?: number }>} rows
   * @param {"item" | "task"} kind
   * @returns {void}
   */
  function renderDraftRows(rowsEl, rows, kind) {
    if (!rowsEl) return;
    rowsEl.innerHTML = "";
    if (rows.length === 0) {
      rowsEl.innerHTML = `<p class="modal-empty">No ${kind === "item" ? "items" : "tasks"} listed yet.</p>`;
      return;
    }

    rows.forEach((row, index) => {
      const article = document.createElement("article");
      article.className = "eventitem-bring-row";

      const head = document.createElement("div");
      head.className = "eventitem-bring-row-head eventitem-bring-row-head--with-edit";

      const title = document.createElement("h3");
      title.className = "eventitem-bring-row-title";
      title.textContent = row.title;
      head.appendChild(title);

      const editBtn = document.createElement("button");
      editBtn.type = "button";
      editBtn.className = "eventitem-bring-row-edit";
      editBtn.setAttribute("aria-label", kind === "item" ? "Edit item" : "Edit task");
      editBtn.dataset.draftIndex = String(index);
      if (kind === "item") {
        editBtn.dataset.draftEditItem = "1";
      } else {
        editBtn.dataset.draftEditTask = "1";
      }
      editBtn.innerHTML = '<i class="fa-solid fa-pen" aria-hidden="true"></i>';
      head.appendChild(editBtn);
      article.appendChild(head);

      if (kind === "item" && row.amount) {
        const meta = document.createElement("div");
        meta.className = "eventitem-bring-row-meta";
        meta.innerHTML = '<i class="fa-solid fa-scale-balanced" aria-hidden="true"></i>';
        const span = document.createElement("span");
        span.textContent = row.amount;
        meta.appendChild(span);
        article.appendChild(meta);
      }

      if (kind === "task" && (row.time || row.location)) {
        const meta = document.createElement("div");
        meta.className = "eventitem-bring-row-meta eventitem-bring-row-meta-task";
        if (row.time) {
          const timeWrap = document.createElement("span");
          timeWrap.innerHTML = '<i class="fa-regular fa-clock" aria-hidden="true"></i> ';
          timeWrap.append(document.createTextNode(row.time));
          meta.appendChild(timeWrap);
        }
        if (row.location) {
          const locWrap = document.createElement("span");
          locWrap.innerHTML = '<i class="fa-solid fa-location-dot" aria-hidden="true"></i> ';
          locWrap.append(document.createTextNode(row.location));
          meta.appendChild(locWrap);
        }
        article.appendChild(meta);
      }

      rowsEl.appendChild(article);
    });
  }

  /**
   * @returns {void}
   */
  function refreshDraftCounts() {
    const nItems = draft.bringItems.length;
    const nTasks = draft.guestTasks.length;
    if (itemsCountEl) itemsCountEl.textContent = `${nItems} item${nItems === 1 ? "" : "s"}`;
    if (tasksCountEl) tasksCountEl.textContent = `${nTasks} task${nTasks === 1 ? "" : "s"}`;
  }

  document.addEventListener("portal-ui:before-modal-open", (e) => {
    const target = e.detail?.target;
    const trigger = e.detail?.trigger;
    if (trigger?.hasAttribute("data-reset-add-item")) {
      resetDraftItemForm();
    }
    if (trigger?.hasAttribute("data-reset-add-task")) {
      resetDraftTaskForm();
    }
    if (target?.id === "eventitems-modal") {
      syncDraftModalTitles();
      renderDraftRows(itemRowsEl, draft.bringItems, "item");
    }
    if (target?.id === "eventtasks-modal") {
      syncDraftModalTitles();
      renderDraftRows(taskRowsEl, draft.guestTasks, "task");
    }
  });

  document.getElementById("newevent-title")?.addEventListener("input", syncDraftModalTitles);

  itemRowsEl?.addEventListener("click", (e) => {
    const editBtn = e.target.closest("[data-draft-edit-item]");
    if (!editBtn) return;
    e.preventDefault();
    const index = parseInt(editBtn.dataset.draftIndex || "", 10);
    if (!Number.isFinite(index)) return;
    openDraftItemForEdit(index);
  });

  taskRowsEl?.addEventListener("click", (e) => {
    const editBtn = e.target.closest("[data-draft-edit-task]");
    if (!editBtn) return;
    e.preventDefault();
    const index = parseInt(editBtn.dataset.draftIndex || "", 10);
    if (!Number.isFinite(index)) return;
    openDraftTaskForEdit(index);
  });

  if (allowItemsToggle && allowItemsHidden) {
    allowItemsToggle.addEventListener("change", () => {
      allowItemsHidden.value = allowItemsToggle.checked ? "true" : "false";
    });
  }

  if (allowTasksToggle && allowTasksHidden) {
    allowTasksToggle.addEventListener("change", () => {
      allowTasksHidden.value = allowTasksToggle.checked ? "true" : "false";
    });
  }

  addItemForm.addEventListener("submit", (e) => {
    e.preventDefault();
    const name = document.getElementById("eventitem-add-item-name")?.value?.trim() || "";
    if (!name) return;
    const amount = document.getElementById("eventitem-add-item-amount")?.value?.trim() || "";
    let people = parseInt(String(document.getElementById("eventitem-add-item-people")?.value || ""), 10);
    if (!Number.isFinite(people) || people < 1) people = 1;
    const entry = { title: name.toUpperCase(), amount, people };
    if (editingItemIndex >= 0) {
      draft.bringItems[editingItemIndex] = entry;
    } else {
      draft.bringItems.push(entry);
    }
    resetDraftItemForm();
    renderDraftRows(itemRowsEl, draft.bringItems, "item");
    refreshDraftCounts();
    window.PortalUi.setModalVisible(addItemModal, false);
    window.PortalUi.setModalVisible(itemListModal, true);
  });

  if (addTaskForm && addTaskForm.dataset.draftOnly === "true") {
    addTaskForm.addEventListener("submit", (e) => {
      e.preventDefault();
      const name = document.getElementById("eventitem-add-task-name")?.value?.trim() || "";
      if (!name) return;
      const location = document.getElementById("eventitem-add-task-location")?.value?.trim() || "";
      const time = document.getElementById("eventitem-add-task-time")?.value?.trim() || "";
      let people = parseInt(String(document.getElementById("eventitem-add-task-people")?.value || ""), 10);
      if (!Number.isFinite(people) || people < 1) people = 1;
      const entry = { title: name.toUpperCase(), location, time, people };
      if (editingTaskIndex >= 0) {
        draft.guestTasks[editingTaskIndex] = entry;
      } else {
        draft.guestTasks.push(entry);
      }
      resetDraftTaskForm();
      renderDraftRows(taskRowsEl, draft.guestTasks, "task");
      refreshDraftCounts();
      window.PortalUi.setModalVisible(addTaskModal, false);
      window.PortalUi.setModalVisible(taskListModal, true);
    });
  }

  renderDraftRows(itemRowsEl, draft.bringItems, "item");
  renderDraftRows(taskRowsEl, draft.guestTasks, "task");
  refreshDraftCounts();
}

/********************************************************************************/

document.addEventListener("DOMContentLoaded", initNewEventPage);
