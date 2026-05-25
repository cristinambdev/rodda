// *************************************************************************************************
// site.js — Shared portal UI (`_PortalLayout.cshtml`)
// *************************************************************************************************
// Dropdowns, modals, forms, previews, header profile menu (Settings / Log out), bottom-nav active state.
// Exposes `window.PortalUi` so page scripts can open/close modals and submit anti-forgery POST forms.

let profileNavMenuInitialized = false;

const MODAL_BODY_SCROLL_LOCK = {
  "eventitems-modal": "eventitem-items-modal-open",
  "eventtasks-modal": "eventtasks-modal-open",
};

document.addEventListener("DOMContentLoaded", () => {
  if (typeof updateRelativeTimes === "function") {
    updateRelativeTimes();
    setInterval(updateRelativeTimes, 60000);
  }

  // === DROPDOWNS ===
  initDropdowns();

  // === MODALS ===
  initModals();

  // === CUSTOM SELECTS ===
  initCustomSelects();

  // === FORM SUBMISSION HANDLING ===
  initAjaxForms();

  // === IMAGE PREVIEWER ===
  initImagePreviewers();

  // === RESET FORMS IN MODALS ON LOAD ===
  resetModalFormsOnLoad();

  // === PORTAL SHELL (profile menu, bottom nav) ===
  mountPortalShell();

  // === ANCHORED MENUS (hero ⋮, etc.) ===
  initAnchoredMenus();
});

// *************************************************************************************************
// Modal API (used by markup and page scripts)
// *************************************************************************************************

// Toggles modal visibility and optional body scroll-lock class for tall item/task modals.
// @param {HTMLElement | null} modal
// @param {boolean} visible
// @returns {void}
function setModalVisible(modal, visible) {
  if (!modal) return;
  modal.classList.toggle("modal-show", visible);
  const bodyClass = MODAL_BODY_SCROLL_LOCK[modal.id];
  if (bodyClass) {
    document.body.classList.toggle(bodyClass, visible);
  }
}

/********************************************************************************/

// Opens a modal by CSS selector; optionally closes a parent modal first (`data-close-parent`).
// @param {string} targetSelector
// @param {HTMLElement | null} trigger
// @returns {HTMLElement | null}
function openModalBySelector(targetSelector, trigger) {
  const modal = document.querySelector(targetSelector);
  if (!modal) return null;

  const closeParentId = trigger?.getAttribute("data-close-parent");
  if (closeParentId) {
    closeModalBySelector(closeParentId, trigger);
  }

  document.dispatchEvent(
    new CustomEvent("portal-ui:before-modal-open", {
      detail: { trigger: trigger || null, target: modal, targetSelector },
    })
  );

  setModalVisible(modal, true);
  return modal;
}

/********************************************************************************/

// Closes a modal by selector; may reopen another via `data-reopen` on the trigger.
// @param {string} targetSelector
// @param {HTMLElement | null} trigger
// @returns {void}
function closeModalBySelector(targetSelector, trigger) {
  const modal = document.querySelector(targetSelector);
  if (!modal) return;

  document.dispatchEvent(
    new CustomEvent("portal-ui:before-modal-close", {
      detail: { trigger: trigger || null, target: modal, targetSelector },
    })
  );

  setModalVisible(modal, false);

  const reopenId = trigger?.getAttribute("data-reopen");
  if (reopenId) {
    openModalBySelector(reopenId, trigger);
  }
}

/********************************************************************************/

// Closes every visible `.modal.modal-show` (Escape key handler).
// @returns {void}
function closeAllModals() {
  document.querySelectorAll(".modal.modal-show").forEach((modal) => {
    setModalVisible(/** @type {HTMLElement} */ (modal), false);
  });
}

// *************************************************************************************************
// Dropdowns
// *************************************************************************************************

// Wires `[data-type="dropdown"]` toggles and closes open menus on outside click.
// @returns {void}
function initDropdowns() {
  const dropdowns = document.querySelectorAll('[data-type="dropdown"]');

  document.addEventListener("click", (event) => {
    let clickedDropdown = null;

    dropdowns.forEach((dropdown) => {
      const targetId = dropdown.getAttribute("data-target");
      const targetElement = targetId ? document.querySelector(targetId) : null;
      if (!targetElement) return;

      if (dropdown.contains(/** @type {Node} */ (event.target))) {
        clickedDropdown = targetElement;

        document.querySelectorAll(".dropdown.dropdown-show").forEach((openDropdown) => {
          if (openDropdown !== targetElement) {
            openDropdown.classList.remove("dropdown-show");
          }
        });

        targetElement.classList.toggle("dropdown-show");
      }
    });

    if (!clickedDropdown && !/** @type {HTMLElement} */ (event.target).closest(".dropdown")) {
      document.querySelectorAll(".dropdown.dropdown-show").forEach((openDropdown) => {
        openDropdown.classList.remove("dropdown-show");
      });
    }
  });
}

// *************************************************************************************************
// Modals (`data-type="modal"` / `data-type="close"`)
// *************************************************************************************************

// Binds modal open/close triggers, bring-items shortcuts, Escape, and deep-link open on details page.
// @returns {void}
function initModals() {
  const modals = document.querySelectorAll('[data-type="modal"]');
  modals.forEach((modal) => {
    modal.addEventListener("click", function () {
      const targetId = modal.getAttribute("data-target");
      if (targetId) openModalBySelector(targetId, /** @type {HTMLElement} */ (modal));
    });
  });

  const closeButtons = document.querySelectorAll('[data-type="close"]');
  closeButtons.forEach((button) => {
    button.addEventListener("click", function () {
      const targetId = button.getAttribute("data-target");
      if (targetId) closeModalBySelector(targetId, /** @type {HTMLElement} */ (button));
    });
  });

  document.querySelectorAll("[data-open-bring-items-modal]").forEach((btn) => {
    btn.addEventListener("click", (e) => {
      e.preventDefault();
      openModalBySelector("#eventitems-modal", /** @type {HTMLElement} */ (btn));
    });
  });

  document.querySelectorAll("[data-open-eventtasks-modal]").forEach((btn) => {
    btn.addEventListener("click", (e) => {
      e.preventDefault();
      openModalBySelector("#eventtasks-modal", /** @type {HTMLElement} */ (btn));
    });
  });

  document.addEventListener("keydown", (e) => {
    if (e.key === "Escape") closeAllModals();
  });

  const detailsPage = document.getElementById("event-details-page");
  const openModalKind = detailsPage?.dataset.openModal?.trim().toLowerCase();
  if (openModalKind === "items") {
    openModalBySelector("#eventitems-modal", null);
  } else if (openModalKind === "tasks") {
    openModalBySelector("#eventtasks-modal", null);
  }
}

// *************************************************************************************************
// Custom selects
// *************************************************************************************************

// Initializes one custom select: hidden input sync, placeholder styling, click-outside close.
// @param {HTMLElement} select
// @returns {void}
function initializeCustomSelect(select) {
  const trigger = select.querySelector(".custom-select-trigger");
  const triggerText = select.querySelector(".custom-select-text");
  const options = select.querySelectorAll(".custom-select-option");
  const hiddenInput = /** @type {HTMLInputElement | null} */ (select.querySelector('input[type="hidden"]'));
  const placeholder = select.getAttribute("data-placeholder") || "Choose Status";

  if (!trigger || !triggerText || !hiddenInput) return;

  const setValue = (value = "", text = placeholder) => {
    triggerText.textContent = text;
    hiddenInput.value = value;
    select.classList.toggle("has-placeholder", !value);
  };

  const currentValue = hiddenInput.value;
  if (currentValue) {
    const matchingOption = Array.from(options).find((option) => option.dataset.value === currentValue);
    if (matchingOption) {
      setValue(currentValue, matchingOption.textContent || "");
    } else {
      setValue();
    }
  } else {
    setValue();
  }

  trigger.addEventListener("click", (e) => {
    e.stopPropagation();
    document.querySelectorAll(".custom-select.open").forEach((el) => {
      if (el !== select) el.classList.remove("open");
    });
    select.classList.toggle("open");
  });

  options.forEach((option) => {
    option.addEventListener("click", () => {
      setValue(option.dataset.value || "", option.textContent || "");
      select.classList.remove("open");
    });
  });

  document.addEventListener("click", (e) => {
    if (!select.contains(/** @type {Node} */ (e.target))) {
      select.classList.remove("open");
    }
  });
}

/********************************************************************************/

// Initializes every `.custom-select` on the page.
// @returns {void}
function initCustomSelects() {
  document.querySelectorAll(".custom-select").forEach((select) => {
    initializeCustomSelect(/** @type {HTMLElement} */ (select));
  });
}

// *************************************************************************************************
// Ajax forms (modal forms and `[data-ajax-form]` only; skips `.no-ajax`)
// *************************************************************************************************

// Inline required-field validation for portal modal forms.
// @param {HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement} field
// @returns {void}
function validatePortalField(field) {
  const errorSpan = field.closest(".form-group")?.querySelector(".error-message");
  if (!errorSpan) return;

  const value = "value" in field ? String(field.value).trim() : "";
  if (!value) {
    errorSpan.textContent = `${field.name} is required.`;
    field.classList.add("input-error");
  } else {
    errorSpan.textContent = "";
    field.classList.remove("input-error");
  }
}

/********************************************************************************/

// Clears ASP.NET validation classes before a new AJAX submit attempt.
// @param {HTMLFormElement} form
// @returns {void}
function clearPortalFormErrors(form) {
  form.querySelectorAll('[data-val="true"]').forEach((input) => {
    input.classList.remove("input-validation-error");
  });
  form.querySelectorAll("[data-valmsg-for]").forEach((span) => {
    span.textContent = "";
    span.classList.remove("field-validation-error");
  });
}

/********************************************************************************/

// Intercepts modal and `[data-ajax-form]` submits; reloads on success, maps 400 JSON errors to fields.
// @returns {void}
function initAjaxForms() {
  const forms = document.querySelectorAll(
    ".modal form:not(.no-ajax), [data-ajax-form] form:not(.no-ajax)"
  );

  forms.forEach((form) => {
    const fields = form.querySelectorAll(
      "input[data-val='true'], select[data-val='true'], textarea[data-val='true']"
    );

    fields.forEach((field) => {
      field.addEventListener("input", () => {
        validatePortalField(/** @type {HTMLInputElement} */ (field));
      });
      field.addEventListener("blur", () => {
        validatePortalField(/** @type {HTMLInputElement} */ (field));
      });
      if (/** @type {HTMLInputElement} */ (field).type === "checkbox") {
        field.addEventListener("change", () => {
          validatePortalField(/** @type {HTMLInputElement} */ (field));
        });
      }
    });

    form.addEventListener("submit", async (e) => {
      e.preventDefault();
      clearPortalFormErrors(/** @type {HTMLFormElement} */ (form));

      document.querySelectorAll(".custom-select").forEach((select) => {
        const selectedOption = select.querySelector(".custom-select-option.selected");
        const hiddenInput = select.querySelector('input[type="hidden"]');
        if (selectedOption && hiddenInput) {
          /** @type {HTMLInputElement} */ (hiddenInput).value =
            selectedOption.dataset.value || "";
        }
      });

      const formData = new FormData(/** @type {HTMLFormElement} */ (form));
      const method =
        form.dataset.method || form.getAttribute("method") || "post";

      try {
        const res = await fetch(form.action, {
          method: method.toLowerCase(),
          body: formData,
          headers: { Accept: "application/json" },
        });

        if (res.ok) {
          const modalElement = form.closest(".modal");
          if (modalElement) {
            setModalVisible(/** @type {HTMLElement} */ (modalElement), false);
          }
          window.location.reload();
        } else if (res.status === 400) {
          const data = await res.json();
          if (data.errors) {
            Object.keys(data.errors).forEach((key) => {
              const input = form.querySelector(`[name="${key}"]`);
              const span = form.querySelector(`[data-valmsg-for="${key}"]`);
              if (input) input.classList.add("input-validation-error");
              if (span) {
                span.classList.remove("field-validation-valid");
                span.classList.add("field-validation-error");
                span.textContent = data.errors[key].join("\n");
              }
            });
          }
        } else {
          const errorText = await res.text();
          console.error("Form submission failed:", res.status, errorText);
        }
      } catch (err) {
        console.error("Error submitting form:", err);
      }
    });
  });
}

// *************************************************************************************************
// Image previewer
// *************************************************************************************************

// Reads a file into an `Image` for square canvas preview.
// @param {File} file
// @returns {Promise<HTMLImageElement>}
async function loadPreviewImage(file) {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onerror = () => reject(new Error("Failed to load file."));
    reader.onload = (e) => {
      const img = new Image();
      img.onerror = () => reject(new Error("Failed to load image."));
      img.onload = () => resolve(img);
      img.src = /** @type {string} */ (e.target?.result);
    };
    reader.readAsDataURL(file);
  });
}

/********************************************************************************/

// Draws a centered square JPEG preview into the image previewer shell.
// @param {File} file
// @param {HTMLImageElement} imagePreview
// @param {HTMLElement} previewer
// @param {number} [previewSize]
// @returns {Promise<void>}
async function processPreviewImage(file, imagePreview, previewer, previewSize = 150) {
  const img = await loadPreviewImage(file);
  const canvas = document.createElement("canvas");
  canvas.width = previewSize;
  canvas.height = previewSize;
  const ctx = canvas.getContext("2d");
  if (!ctx) return;
  ctx.drawImage(img, 0, 0, previewSize, previewSize);
  imagePreview.src = canvas.toDataURL("image/jpeg");
  previewer.classList.add("selected");
}

/********************************************************************************/

// Wires click-to-browse on `.image-previewer` blocks.
// @returns {void}
function initImagePreviewers() {
  const previewSize = 150;

  document.querySelectorAll(".image-previewer").forEach((previewer) => {
    const fileInput = /** @type {HTMLInputElement | null} */ (
      previewer.querySelector('input[type="file"]')
    );
    const imagePreview = /** @type {HTMLImageElement | null} */ (
      previewer.querySelector(".image-preview")
    );
    if (!fileInput || !imagePreview) return;

    previewer.addEventListener("click", () => fileInput.click());

    fileInput.addEventListener("change", ({ target }) => {
      const input = /** @type {HTMLInputElement} */ (target);
      const file = input.files?.[0];
      if (file) {
        processPreviewImage(file, imagePreview, /** @type {HTMLElement} */ (previewer), previewSize);
      }
    });
  });
}

// *************************************************************************************************
// Modal form reset (initial page load)
// *************************************************************************************************

// Clears modal forms and image previews on first paint so stale create-modal state does not linger.
// @returns {void}
function resetModalFormsOnLoad() {
  document.querySelectorAll(".modal").forEach((modal) => {
    modal.querySelectorAll("form").forEach((form) => {
      form.reset();

      const imagePreview = form.querySelector(".image-preview");
      if (imagePreview) {
        /** @type {HTMLImageElement} */ (imagePreview).src = "";
      }

      const imagePreviewer = form.querySelector(".image-previewer");
      if (imagePreviewer) {
        imagePreviewer.classList.remove("selected");
      }
    });
  });
}

// *************************************************************************************************
// Anchored menus (`[data-menu-toggle]` + `[aria-controls]` / `[data-menu-target]`)
// *************************************************************************************************

// Reads the MVC anti-forgery token injected in layout markup.
// @returns {string}
function getAntiForgeryToken() {
  return document.querySelector('input[name="__RequestVerificationToken"]')?.value || "";
}

/********************************************************************************/

// Builds and submits a transient POST form (used for logout and destructive actions).
// @param {string} action
// @param {Record<string, string>} [fields]
// @returns {void}
function submitPostForm(action, fields = {}) {
  const token = getAntiForgeryToken();
  const form = document.createElement("form");
  form.method = "POST";
  form.action = action;

  if (token) {
    const tokenInput = document.createElement("input");
    tokenInput.type = "hidden";
    tokenInput.name = "__RequestVerificationToken";
    tokenInput.value = token;
    form.appendChild(tokenInput);
  }

  Object.entries(fields).forEach(([name, value]) => {
    const input = document.createElement("input");
    input.type = "hidden";
    input.name = name;
    input.value = value;
    form.appendChild(input);
  });

  document.body.appendChild(form);
  form.submit();
}

/********************************************************************************/

// Wires `[data-menu-toggle]` popovers with click-outside and Escape dismiss.
// @returns {void}
function initAnchoredMenus() {
  document.querySelectorAll("[data-menu-toggle]").forEach((trigger) => {
    if (trigger.dataset.menuToggleBound === "1") return;

    const menuId =
      trigger.getAttribute("aria-controls") || trigger.getAttribute("data-menu-target");
    const menu = menuId ? document.getElementById(menuId) : null;
    if (!menu) return;

    trigger.dataset.menuToggleBound = "1";
    const wrap =
      trigger.closest("[data-menu-wrap]") ||
      trigger.parentElement;

    // @returns {void}
    function closeMenu() {
      menu.hidden = true;
      trigger.setAttribute("aria-expanded", "false");
    }

    // @returns {void}
    function openMenu() {
      menu.hidden = false;
      trigger.setAttribute("aria-expanded", "true");
    }

    trigger.addEventListener("click", (event) => {
      event.preventDefault();
      event.stopPropagation();
      if (!menu.hidden) {
        closeMenu();
        return;
      }
      openMenu();
    });

    document.addEventListener("click", (event) => {
      const target = /** @type {Node} */ (event.target);
      if (wrap?.contains(target)) return;
      closeMenu();
    });

    document.addEventListener("keydown", (event) => {
      if (event.key === "Escape") closeMenu();
    });
  });
}

// ********************************************************************************/

window.PortalUi = {
  openModal: openModalBySelector,
  closeModal: closeModalBySelector,
  setModalVisible,
  closeAllModals,
  getAntiForgeryToken,
  submitPostForm,
};

// *************************************************************************************************
// Portal shell (profile popover, bottom nav, logout)
// *************************************************************************************************

// Initializes profile menu + bottom-nav active state from layout dataset.
// @param {{ activeNav?: string }} [options]
// @returns {void}
function mountPortalShell(options) {
  initProfileNavMenu();
  applyPortalNavActive(options?.activeNav || document.body.dataset.activeNav || "");
}

/********************************************************************************/

// Highlights the active bottom-nav link for the current page.
// @param {string} activeNav
// @returns {void}
function applyPortalNavActive(activeNav) {
  if (!activeNav) return;
  const menu = document.querySelector("#navigation-menu, nav.navigation-menu");
  if (!menu) return;

  menu.querySelectorAll("[data-nav]").forEach((link) => {
    if (link.classList.contains("fab")) return;
    const id = link.getAttribute("data-nav");
    const on = id === activeNav;
    link.classList.toggle("navlink-active", on);
    link.classList.toggle("app-bottom-nav-link-active", on);
  });
}

/********************************************************************************/

// Builds the header profile popover (Settings + Log out) once per page load.
// @returns {void}
function initProfileNavMenu() {
  if (profileNavMenuInitialized) return;

  const profileBtn = /** @type {HTMLElement | null} */ (document.querySelector("[data-header-profile]"));
  const profileGroup = profileBtn?.closest(".header-profile-group");
  if (!profileBtn || profileBtn.hidden || (profileGroup instanceof HTMLElement && profileGroup.hidden)) return;

  const shell = document.body;
  const settingsUrl = shell.dataset.settingsUrl || "/settings";
  const signInUrl = shell.dataset.signInUrl || "/Auth/SignIn";

  const menu = document.createElement("div");
  menu.className = "profile-nav-popover";
  menu.setAttribute("role", "menu");
  menu.hidden = true;
  menu.innerHTML = `
    <a class="profile-nav-popover-item" role="menuitem" href="${settingsUrl}" data-profile-menu-settings>
      <i class="fa-solid fa-gear" aria-hidden="true"></i>
      <span>Settings</span>
    </a>
    <div class="profile-nav-popover-sep" aria-hidden="true"></div>
    <button
      type="button"
      class="profile-nav-popover-item profile-nav-popover-item--danger"
      role="menuitem"
      data-profile-menu-logout
    >
      <i class="fa-solid fa-right-from-bracket" aria-hidden="true"></i>
      <span>Log out</span>
    </button>
  `;
  document.body.appendChild(menu);

  function closeMenu() {
    menu.hidden = true;
    profileBtn.setAttribute("aria-expanded", "false");
  }

  function openMenu() {
    const rect = profileBtn.getBoundingClientRect();
    const estimatedWidth = 160;
    const left = Math.max(8, Math.min(rect.right - estimatedWidth, window.innerWidth - estimatedWidth - 8));
    menu.style.left = `${left}px`;
    menu.style.top = `${rect.bottom + 6}px`;
    menu.hidden = false;
    profileBtn.setAttribute("aria-expanded", "true");
  }

  profileBtn.setAttribute("aria-haspopup", "menu");
  profileBtn.setAttribute("aria-expanded", "false");
  profileBtn.addEventListener("click", (e) => {
    e.preventDefault();
    if (menu.hidden) openMenu();
    else closeMenu();
  });

  menu.querySelector("[data-profile-menu-settings]")?.addEventListener("click", () => {
    closeMenu();
  });

  menu.querySelector("[data-profile-menu-logout]")?.addEventListener("click", () => {
    closeMenu();
    submitPortalLogout(signInUrl);
  });

  document.addEventListener("click", (e) => {
    const target = /** @type {HTMLElement} */ (e.target);
    if (target.closest(".profile-nav-popover")) return;
    if (target.closest("[data-header-profile], .header-profile-group")) return;
    closeMenu();
  });

  document.addEventListener("keydown", (e) => {
    if (e.key === "Escape") closeMenu();
  });

  profileNavMenuInitialized = true;
}

/********************************************************************************/

// Submits the hidden logout form when present; otherwise navigates to sign-in.
// @param {string} signInUrl
// @returns {void}
function submitPortalLogout(signInUrl) {
  const form = /** @type {HTMLFormElement | null} */ (document.getElementById("portal-logout-form"));
  if (form) {
    form.requestSubmit();
    return;
  }
  window.location.href = signInUrl;
}

/********************************************************************************/
