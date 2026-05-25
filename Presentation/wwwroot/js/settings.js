// *************************************************************************************************
// settings.js — Settings page (`Settings/Index.cshtml`)
// *************************************************************************************************
// - Profile form posts to `SettingsController.UpdateProfile` (name, email, avatar, clear).
// - Other sections remain UI-only until their endpoints exist.
// - Uses `PortalUi.getAntiForgeryToken` from `site.js` when the form token is absent.

const SETTINGS_LANGUAGE_LABELS = {
  en: "English",
  sv: "Swedish",
  no: "Norwegian",
  es: "Spanish",
};

// *************************************************************************************************
// API helpers
// *************************************************************************************************
// @param {string} url
// @param {FormData} body
// @returns {Promise<{ ok: boolean, data: Record<string, unknown>, status: number }>}
async function postSettingsForm(url, body) {
  const token =
    (typeof window.PortalUi !== "undefined" && window.PortalUi.getAntiForgeryToken
      ? window.PortalUi.getAntiForgeryToken()
      : "") || "";

  if (token && !body.has("__RequestVerificationToken")) {
    body.append("__RequestVerificationToken", token);
  }

  const res = await fetch(url, {
    method: "POST",
    body,
    headers: { Accept: "application/json" },
  });

  let data = {};
  try {
    data = await res.json();
  } catch {
    data = {};
  }

  return { ok: res.ok, data, status: res.status };
}

/********************************************************************************/
// @param {HTMLElement | null} el
// @param {string} message
// @param {"error" | "ok" | null} tone
// @returns {void}
function setDialogFeedback(el, message, tone) {
  if (!el) return;
  if (!message) {
    el.hidden = true;
    el.textContent = "";
    el.removeAttribute("data-settings-feedback-tone");
    return;
  }
  el.hidden = false;
  el.textContent = message;
  if (tone) el.setAttribute("data-settings-feedback-tone", tone);
  else el.removeAttribute("data-settings-feedback-tone");
}

// *************************************************************************************************
// Page bootstrap
// *************************************************************************************************
// Entry point for the settings view; each helper no-ops when its target markup is missing.
// @returns {void}
function initSettingsPage() {
  initSettingsProfileForm();
  initSettingsFormGuards();
  initSettingsAvatarPreview();
  initSettingsLanguagePicker();
  initSettingsPasswordToggles();
  initSettingsDeleteAccountDialog();
}

// *************************************************************************************************
// Profile form (persisted)
// *************************************************************************************************

// @returns {void}
function initSettingsProfileForm() {
  const form = document.querySelector("[data-settings-profile-form]");
  if (!(form instanceof HTMLFormElement)) return;

  const feedback = document.querySelector("[data-settings-profile-feedback]");
  const saveBtn = form.querySelector(".settings-profile-save");
  const updateUrl = form.getAttribute("data-settings-update-url") || form.action;
  if (!updateUrl) return;

  form.addEventListener("submit", async (e) => {
    e.preventDefault();
    setDialogFeedback(/** @type {HTMLElement | null} */ (feedback), "", null);

    const displayNameInput = /** @type {HTMLInputElement | null} */ (
      form.querySelector('[name="displayName"]')
    );
    const emailInput = /** @type {HTMLInputElement | null} */ (form.querySelector('[name="email"]'));
    const displayName = String(displayNameInput?.value || "").trim();
    const email = String(emailInput?.value || "").trim();

    if (!displayName) {
      setDialogFeedback(/** @type {HTMLElement | null} */ (feedback), "Display name is required.", "error");
      displayNameInput?.focus();
      return;
    }
    if (!email) {
      setDialogFeedback(/** @type {HTMLElement | null} */ (feedback), "Email is required.", "error");
      emailInput?.focus();
      return;
    }

    const body = new FormData(form);
    const defaultLabel = saveBtn?.textContent || "Save profile";
    if (saveBtn instanceof HTMLButtonElement) {
      saveBtn.disabled = true;
      saveBtn.textContent = "Saving…";
    }

    const { ok, data } = await postSettingsForm(updateUrl, body);

    if (saveBtn instanceof HTMLButtonElement) {
      saveBtn.disabled = false;
      saveBtn.textContent = defaultLabel;
    }

    if (!ok) {
      setDialogFeedback(
        /** @type {HTMLElement | null} */ (feedback),
        typeof data.errorMessage === "string" ? data.errorMessage : "Could not save profile.",
        "error"
      );
      return;
    }

    if (displayNameInput && typeof data.displayName === "string") {
      displayNameInput.value = data.displayName;
    }
    if (emailInput && typeof data.email === "string") {
      emailInput.value = data.email;
    }

    const img = /** @type {HTMLImageElement | null} */ (document.getElementById("settings-avatar-image"));
    const avatarInput = /** @type {HTMLInputElement | null} */ (
      document.getElementById("settings-avatar-input")
    );
    const profileUrl =
      typeof data.profileImageUrl === "string" ? data.profileImageUrl.trim() : "";

    if (img && profileUrl) {
      img.src = profileUrl;
      setSettingsAvatarVisible(true);
    } else if (img) {
      setSettingsAvatarVisible(false);
    }

    if (avatarInput) avatarInput.value = "";

    const clearInput = /** @type {HTMLInputElement | null} */ (
      document.getElementById("settings-clear-avatar")
    );
    if (clearInput) clearInput.value = "0";

    setDialogFeedback(
      /** @type {HTMLElement | null} */ (feedback),
      "Profile saved.",
      "ok"
    );

    window.setTimeout(() => {
      window.location.reload();
    }, 600);
  });
}

// *************************************************************************************************
// Form guards (notifications / account — not wired yet)
// *************************************************************************************************

// @returns {void}
function initSettingsFormGuards() {
  document
    .querySelectorAll("[data-settings-notifications-form], [data-settings-account-form]")
    .forEach((form) => {
      if (!(form instanceof HTMLFormElement)) return;
      form.addEventListener("submit", (e) => {
        e.preventDefault();
      });
    });

  const passwordSubmit = document.querySelector(".settings-password-submit");
  passwordSubmit?.addEventListener("click", (e) => {
    e.preventDefault();
    const feedback = document.querySelector("[data-settings-password-feedback]");
    setDialogFeedback(
      /** @type {HTMLElement | null} */ (feedback),
      "Password change is not connected yet.",
      null
    );
  });
}

// *************************************************************************************************
// Avatar preview
// *************************************************************************************************

// Toggles avatar preview, placeholder, and remove control together for consistent UI state.
// @param {boolean} hasImage
// @returns {void}
function setSettingsAvatarVisible(hasImage) {
  const img = /** @type {HTMLImageElement | null} */ (document.getElementById("settings-avatar-image"));
  const placeholder = document.getElementById("settings-avatar-placeholder");
  const removeBtn = document.querySelector("[data-settings-avatar-remove]");
  const clearInput = /** @type {HTMLInputElement | null} */ (
    document.getElementById("settings-clear-avatar")
  );

  if (img) {
    img.hidden = !hasImage;
    if (!hasImage) img.removeAttribute("src");
  }
  if (placeholder) placeholder.hidden = hasImage;
  if (removeBtn instanceof HTMLElement) removeBtn.hidden = !hasImage;
  if (clearInput) clearInput.value = hasImage ? "0" : "1";
}

/********************************************************************************/

// @returns {void}
function initSettingsAvatarPreview() {
  const input = /** @type {HTMLInputElement | null} */ (document.getElementById("settings-avatar-input"));
  const img = /** @type {HTMLImageElement | null} */ (document.getElementById("settings-avatar-image"));
  const removeBtn = document.querySelector("[data-settings-avatar-remove]");
  if (!input || !img) return;

  const hasInitialImage = !img.hidden && Boolean(img.getAttribute("src"));
  setSettingsAvatarVisible(hasInitialImage);

  input.addEventListener("change", () => {
    const file = input.files?.[0];
    if (!file || !file.type.startsWith("image/")) return;
    if (file.size > 200 * 1024) {
      window.alert("Please choose an image under 200KB.");
      input.value = "";
      return;
    }
    const reader = new FileReader();
    reader.onload = () => {
      const url = typeof reader.result === "string" ? reader.result : "";
      if (!url) return;
      img.src = url;
      setSettingsAvatarVisible(true);
    };
    reader.readAsDataURL(file);
  });

  removeBtn?.addEventListener("click", (e) => {
    e.preventDefault();
    e.stopPropagation();
    input.value = "";
    setSettingsAvatarVisible(false);
  });
}

// *************************************************************************************************
// Language picker
// *************************************************************************************************

// @returns {void}
function initSettingsLanguagePicker() {
  const buttons = /** @type {NodeListOf<HTMLButtonElement>} */ (
    document.querySelectorAll("[data-settings-language]")
  );
  const currentLabel = document.getElementById("settings-language-current");
  const hiddenInput = /** @type {HTMLInputElement | null} */ (
    document.getElementById("settings-language-input")
  );
  if (!buttons.length) return;

  function applySelection(code) {
    buttons.forEach((b) => {
      const on = b.getAttribute("data-settings-language") === code;
      b.setAttribute("aria-pressed", on ? "true" : "false");
    });
    if (currentLabel) {
      currentLabel.textContent = SETTINGS_LANGUAGE_LABELS[code] || "English";
    }
    if (hiddenInput) hiddenInput.value = code;
  }

  const initial = hiddenInput?.value || "en";
  applySelection(initial);

  buttons.forEach((b) => {
    b.addEventListener("click", () => {
      const code = b.getAttribute("data-settings-language") || "en";
      applySelection(code);
    });
  });
}


// *************************************************************************************************
// Password show/hide toggles
// *************************************************************************************************

// @returns {void}
function initSettingsPasswordToggles() {
  document.querySelectorAll("[data-password-toggle]").forEach((btn) => {
    if (!(btn instanceof HTMLButtonElement)) return;
    const id = btn.getAttribute("aria-controls");
    const input = id ? /** @type {HTMLInputElement | null} */ (document.getElementById(id)) : null;
    const icon = btn.querySelector("i");
    if (!input || !icon) return;

    btn.addEventListener("click", () => {
      const showing = input.type === "password";
      input.type = showing ? "text" : "password";
      btn.setAttribute("aria-label", showing ? "Hide password" : "Show password");
      icon.classList.toggle("fa-eye", showing);
      icon.classList.toggle("fa-eye-slash", !showing);
    });
  });
}

// *************************************************************************************************
// Delete account confirmation (dialog UI only)
// *************************************************************************************************

// @returns {void}
function initSettingsDeleteAccountDialog() {
  const trigger = document.querySelector("[data-open-delete-account-dialog]");
  const dialog = /** @type {HTMLDialogElement | null} */ (
    document.getElementById("settings-delete-account-dialog")
  );
  if (!trigger || !dialog) return;

  function closeDialog() {
    if (typeof dialog.close === "function" && dialog.open) dialog.close();
  }

  trigger.addEventListener("click", () => {
    if (typeof dialog.showModal === "function") dialog.showModal();
  });

  dialog
    .querySelectorAll("[data-settings-delete-account-close]")
    .forEach((btn) => btn.addEventListener("click", closeDialog));

  dialog.addEventListener("click", (e) => {
    if (e.target === dialog) closeDialog();
  });

  dialog.querySelector("[data-settings-delete-confirm]")?.addEventListener("click", () => {
    closeDialog();
  });
}

/********************************************************************************/
