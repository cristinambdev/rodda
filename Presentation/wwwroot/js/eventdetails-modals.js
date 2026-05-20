document.addEventListener('DOMContentLoaded', () => {
    const modalsRoot = document.getElementById('eventitem-modals-root')
    if (!modalsRoot) return

    const itemListModal = document.getElementById('eventitems-modal')
    const taskListModal = document.getElementById('eventtasks-modal')
    const addItemModal = document.getElementById('eventitem-add-item-modal')
    const addTaskModal = document.getElementById('eventitem-add-task-modal')
    const addItemForm = document.getElementById('eventitem-add-item-form')
    const addTaskForm = document.getElementById('eventitem-add-task-form')

    const scrollLockByModalId = {
        'eventitems-modal': 'eventitem-items-modal-open',
        'eventtasks-modal': 'eventtasks-modal-open'
    }

    applyManageGates()

    function applyManageGates() {
        const itemModal = document.getElementById('eventitems-modal')
        const taskModal = document.getElementById('eventtasks-modal')
        const canManageItems = itemModal?.dataset.canManage === 'true'
        const canManageTasks = taskModal?.dataset.canManage === 'true'

        if (!canManageItems) {
            document.getElementById('eventitems-modal-add')?.remove()
            document.getElementById('eventitem-items-guest-policy-row')?.remove()
        }
        if (!canManageTasks) {
            document.getElementById('eventtasks-modal-add')?.remove()
            document.getElementById('eventtasks-guest-policy-row')?.remove()
        }
    }

    // Toggles modal-show and optional body scroll lock used by eventitem.css.
    function setModalVisible(modal, visible) {
        if (!modal) return
        modal.classList.toggle('modal-show', visible)
        const bodyClass = scrollLockByModalId[modal.id]
        if (bodyClass) {
            document.body.classList.toggle(bodyClass, visible)
        }
    }

    function openModalBySelector(targetSelector) {
        const modal = document.querySelector(targetSelector)
        if (modal) setModalVisible(modal, true)
    }

    function closeModalBySelector(targetSelector) {
        const modal = document.querySelector(targetSelector)
        if (modal) setModalVisible(modal, false)
    }

    // === MODALS ===
    const modalTriggers = document.querySelectorAll('[data-type="modal"]')

    modalTriggers.forEach(modal => {
        modal.addEventListener('click', function () {
            const targetId = modal.getAttribute('data-target')
            const closeParentId = modal.getAttribute('data-close-parent')

            if (modal.hasAttribute('data-reset-add-item')) {
                resetAddItemForm()
            }
            if (modal.hasAttribute('data-reset-add-task')) {
                resetAddTaskForm()
            }
            if (closeParentId) {
                closeModalBySelector(closeParentId)
            }
            if (targetId) {
                openModalBySelector(targetId)
            }
        })
    })

    const closeButtons = document.querySelectorAll('[data-type="close"]')
    closeButtons.forEach(button => {
        button.addEventListener('click', function () {
            const targetId = button.getAttribute('data-target')
            const reopenId = button.getAttribute('data-reopen')

            if (targetId) {
                closeModalBySelector(targetId)
            }
            if (reopenId) {
                openModalBySelector(reopenId)
            }
        })
    })

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

        setModalVisible(itemListModal, false)
        setModalVisible(addItemModal, true)
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

        setModalVisible(taskListModal, false)
        setModalVisible(addTaskModal, true)
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

    // === FORM SUBMISSION (add / edit item & task) ===
    const forms = modalsRoot.querySelectorAll('form:not(.no-ajax)')

    forms.forEach(form => {
        form.addEventListener('submit', async (e) => {
            e.preventDefault()

            const formData = new FormData(form)

            try {
                const method = form.dataset.method || form.getAttribute('method') || 'post'

                const res = await fetch(form.action, {
                    method: method.toLowerCase(),
                    body: formData,
                    headers: {
                        'Accept': 'application/json'
                    }
                })

                if (res.ok) {
                    const modalElement = form.closest('.modal')
                    if (modalElement) {
                        modalElement.classList.remove('modal-show')
                        const bodyClass = scrollLockByModalId[modalElement.id]
                        if (bodyClass) document.body.classList.remove(bodyClass)
                    }
                    window.location.reload()
                } else if (res.status === 400) {
                    const data = await res.json()
                    if (data.errors) {
                        Object.keys(data.errors).forEach(key => {
                            const input = form.querySelector(`[name="${key}"]`)
                            if (input) {
                                input.classList.add('input-validation-error')
                            }
                        })
                    }
                } else {
                    const errorText = await res.text()
                    console.error('Form submission failed:', res.status, errorText)
                }
            } catch (err) {
                console.error('Error submitting form:', err)
            }
        })
    })
})
