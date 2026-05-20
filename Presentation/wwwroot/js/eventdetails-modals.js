// === GUEST POLICY SETTINGS (EventsController.Update / update-settings) ===

function isEventModalDraft() {
    return document.getElementById('eventitem-modals-root')?.dataset.eventModalDraft === 'true'
}

function isPersistableEventId(eventId) {
    return Boolean(eventId) && eventId !== 'create'
}

// Syncs guest policy toggles with the backend (EditEventViewModel + update-settings).
async function updateEventSettings() {
    if (isEventModalDraft()) {
        return
    }

    const eventId = getEventIdFromPath()
    if (!isPersistableEventId(eventId)) {
        console.error('Could not resolve Event ID from the URL pathway configuration.')
        return
    }

    const allowItemsCheckbox = document.getElementById('toggle-allow-items')
    const allowTasksCheckbox = document.getElementById('toggle-allow-tasks')

    if (!allowItemsCheckbox || !allowTasksCheckbox) return

    const previousItems = allowItemsCheckbox.checked
    const previousTasks = allowTasksCheckbox.checked

    const payload = {
        Id: eventId,
        AllowGuestBringItems: allowItemsCheckbox.checked,
        AllowGuestTasks: allowTasksCheckbox.checked
    }

    const tokenElement = document.querySelector('input[name="__RequestVerificationToken"]')
    if (!tokenElement) {
        console.error('Anti-forgery token missing from the active document workspace.')
        return
    }

    try {
        const response = await fetch(`/events/${encodeURIComponent(eventId)}/update-settings`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Accept': 'application/json',
                'RequestVerificationToken': tokenElement.value,
                'X-XSRF-TOKEN': tokenElement.value
            },
            body: JSON.stringify(payload)
        })

        if (!response.ok) {
            const errorData = await response.json().catch(() => ({}))
            throw new Error(errorData.error || `HTTP error! status: ${response.status}`)
        }

        const data = await response.json()
        if (data.succeeded) {
            window.location.reload()
        }
    } catch (error) {
        console.error('Failed to persist event toggles state:', error)
        alert(`Failed to save settings: ${error.message}`)
        allowItemsCheckbox.checked = previousItems
        allowTasksCheckbox.checked = previousTasks
    }
}

function bindGuestPolicyToggles() {
    if (isEventModalDraft()) {
        return
    }

    const allowItemsCheckbox = document.getElementById('toggle-allow-items')
    const allowTasksCheckbox = document.getElementById('toggle-allow-tasks')

    if (allowItemsCheckbox && !allowItemsCheckbox.dataset.guestPolicyBound) {
        allowItemsCheckbox.dataset.guestPolicyBound = '1'
        allowItemsCheckbox.addEventListener('change', updateEventSettings)
    }
    if (allowTasksCheckbox && !allowTasksCheckbox.dataset.guestPolicyBound) {
        allowTasksCheckbox.dataset.guestPolicyBound = '1'
        allowTasksCheckbox.addEventListener('change', updateEventSettings)
    }
}

// === SHARE LINK (private event access via reusable revocable token) ===

function getEventIdFromPath() {
    const pathSegments = window.location.pathname.split('/')
    return pathSegments[pathSegments.indexOf('events') + 1] || ''
}

function getAntiForgeryToken() {
    return document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
}

async function copyShareLink() {
    const eventId = getEventIdFromPath()
    if (!eventId) return

    const statusEl = document.getElementById('event-details-share-status')
    try {
        const response = await fetch(`/events/${encodeURIComponent(eventId)}/share-link`, {
            headers: { Accept: 'application/json' }
        })

        if (!response.ok) {
            throw new Error('Could not load share link.')
        }

        const data = await response.json()
        if (!data.url) {
            throw new Error('Share link was empty.')
        }

        await navigator.clipboard.writeText(data.url)
        if (statusEl) {
            statusEl.hidden = false
            statusEl.textContent = 'Link copied. Anyone with this link can join while it stays active.'
        }
    } catch (error) {
        console.error('Share link copy failed:', error)
        if (statusEl) {
            statusEl.hidden = false
            statusEl.textContent = 'Could not copy the share link.'
        }
    }
}

async function revokeShareLink() {
    const eventId = getEventIdFromPath()
    if (!eventId) return

    const token = getAntiForgeryToken()
    const statusEl = document.getElementById('event-details-share-status')

    try {
        const response = await fetch(`/events/${encodeURIComponent(eventId)}/share-link/revoke`, {
            method: 'POST',
            headers: {
                Accept: 'application/json',
                RequestVerificationToken: token,
                'X-XSRF-TOKEN': token
            }
        })

        if (!response.ok) {
            throw new Error('Could not revoke share link.')
        }

        if (statusEl) {
            statusEl.hidden = false
            statusEl.textContent = 'Share link revoked. Generate a new one with Copy share link.'
        }
    } catch (error) {
        console.error('Share link revoke failed:', error)
        if (statusEl) {
            statusEl.hidden = false
            statusEl.textContent = 'Could not revoke the share link.'
        }
    }
}

function bindShareDialog() {
    const shareDialog = document.getElementById('event-details-share-dialog')
    if (!shareDialog || shareDialog.dataset.shareBound === '1') return

    shareDialog.dataset.shareBound = '1'
    const canManage = shareDialog.dataset.canManage === 'true'
    const revokeBtn = shareDialog.querySelector('[data-event-details-share-action="revoke"]')
    if (revokeBtn) revokeBtn.hidden = !canManage

    shareDialog.addEventListener('click', (event) => {
        const actionBtn = event.target.closest('[data-event-details-share-action]')
        if (!actionBtn) return

        const action = actionBtn.getAttribute('data-event-details-share-action')
        if (action === 'link') {
            copyShareLink()
            return
        }
        if (action === 'revoke') {
            revokeShareLink()
            return
        }
        if (action === 'cancel') {
            shareDialog.close()
        }
    })

    document.querySelectorAll('[data-event-details-menu-action="share"]').forEach((btn) => {
        btn.addEventListener('click', () => {
            if (typeof shareDialog.showModal === 'function') {
                shareDialog.showModal()
            }
        })
    })
}

document.addEventListener('DOMContentLoaded', () => {
    bindShareDialog()
})

document.addEventListener('DOMContentLoaded', () => {
    const modalsRoot = document.getElementById('eventitem-modals-root')
    if (!modalsRoot) return

    const itemListModal = document.getElementById('eventitems-modal')
    const taskListModal = document.getElementById('eventtasks-modal')
    const addItemModal = document.getElementById('eventitem-add-item-modal')
    const addTaskModal = document.getElementById('eventitem-add-task-modal')
    const addItemForm = document.getElementById('eventitem-add-item-form')
    const addTaskForm = document.getElementById('eventitem-add-task-form')

    applyManageGates()
    bindGuestPolicyToggles()

    document.addEventListener('portal-ui:before-modal-open', (e) => {
        const trigger = e.detail?.trigger
        if (!trigger) return
        if (trigger.hasAttribute('data-reset-add-item')) {
            resetAddItemForm()
        }
        if (trigger.hasAttribute('data-reset-add-task')) {
            resetAddTaskForm()
        }
    })

    function applyManageGates() {
        if (modalsRoot.dataset.eventModalDraft === 'true') {
            return
        }

        const itemModal = document.getElementById('eventitems-modal')
        const taskModal = document.getElementById('eventtasks-modal')
        const canManageItems = itemModal?.dataset.canManage === 'true'
        const canManageTasks = taskModal?.dataset.canManage === 'true'
        const canAddItems = itemModal?.dataset.canAdd === 'true'
        const canAddTasks = taskModal?.dataset.canAdd === 'true'

        if (!canManageItems) {
            document.getElementById('eventitem-items-guest-policy-row')?.remove()
        }
        if (!canManageTasks) {
            document.getElementById('eventtasks-guest-policy-row')?.remove()
        }
        if (!canManageItems && !canAddItems) {
            document.getElementById('eventitems-modal-add')?.remove()
        }
        if (!canManageTasks && !canAddTasks) {
            document.getElementById('eventtasks-modal-add')?.remove()
        }
    }

    // === ADD / EDIT ITEM & TASK FORMS ===
    function resetAddItemForm() {
        if (!addItemForm || addItemForm.dataset.draftOnly === 'true') return
        const createAction = addItemForm.getAttribute('action')
        if (createAction) addItemForm.action = createAction
        const titleEl = document.getElementById('eventitem-add-item-modal-title')
        if (titleEl) titleEl.textContent = 'Add item'
        const idEl = document.getElementById('eventitem-add-item-id')
        if (idEl) idEl.value = ''
        addItemForm.reset()
        const people = document.getElementById('eventitem-add-item-people')
        if (people) people.value = '1'
    }

    function resetAddTaskForm() {
        if (!addTaskForm || addTaskForm.dataset.draftOnly === 'true') return
        const createAction = addTaskForm.getAttribute('action')
        if (createAction) addTaskForm.action = createAction
        const titleEl = document.getElementById('eventitem-add-task-modal-title')
        if (titleEl) titleEl.textContent = 'Add task'
        const idEl = document.getElementById('eventitem-add-task-id')
        if (idEl) idEl.value = ''
        addTaskForm.reset()
        const people = document.getElementById('eventitem-add-task-people')
        if (people) people.value = '1'
    }

    function openAddItemForEdit(btn) {
        if (!addItemForm || !addItemModal) return
        const template = addItemForm.dataset.editUrlTemplate
        const itemId = btn.dataset.itemId
        if (!template || !itemId) return

        addItemForm.action = template.replace('__ITEMID__', encodeURIComponent(itemId))
        document.getElementById('eventitem-add-item-name').value = btn.dataset.itemTitle || ''
        document.getElementById('eventitem-add-item-amount').value = btn.dataset.itemAmount || ''
        document.getElementById('eventitem-add-item-people').value = btn.dataset.itemPeople || '1'
        const idEl = document.getElementById('eventitem-add-item-id')
        if (idEl) idEl.value = itemId
        const titleEl = document.getElementById('eventitem-add-item-modal-title')
        if (titleEl) titleEl.textContent = 'Edit item'

        window.PortalUi.setModalVisible(itemListModal, false)
        window.PortalUi.setModalVisible(addItemModal, true)
    }

    function openAddTaskForEdit(btn) {
        if (!addTaskForm || !addTaskModal) return
        const template = addTaskForm.dataset.editUrlTemplate
        const taskId = btn.dataset.taskId
        if (!template || !taskId) return

        addTaskForm.action = template.replace('__TASKID__', encodeURIComponent(taskId))
        document.getElementById('eventitem-add-task-name').value = btn.dataset.taskTitle || ''
        document.getElementById('eventitem-add-task-time').value = btn.dataset.taskTime || ''
        document.getElementById('eventitem-add-task-location').value = btn.dataset.taskLocation || ''
        document.getElementById('eventitem-add-task-people').value = btn.dataset.taskPeople || '1'
        const idEl = document.getElementById('eventitem-add-task-id')
        if (idEl) idEl.value = taskId
        const titleEl = document.getElementById('eventitem-add-task-modal-title')
        if (titleEl) titleEl.textContent = 'Edit task'

        window.PortalUi.setModalVisible(taskListModal, false)
        window.PortalUi.setModalVisible(addTaskModal, true)
    }

    modalsRoot.addEventListener('click', (e) => {
        const editItem = e.target.closest('[data-edit-item]')
        if (editItem) {
            e.preventDefault()
            openAddItemForEdit(editItem)
            return
        }
        const editTask = e.target.closest('[data-edit-task]')
        if (editTask) {
            e.preventDefault()
            openAddTaskForEdit(editTask)
        }
    })
})
