/**
 * task.js — Task module AJAX logic
 * Handles: list refresh, filters, modal, create, update, delete, quick status change
 */

const TaskModule = (function () {

    let currentPage = 1;
    let currentPageSize = 10;
    let currentSearch = '';
    let currentSort = '';
    let currentSortDesc = false;
    let currentProjectId = '';
    let currentStatus = '';
    let searchTimer = null;
    let pendingDeleteId = null;

    // ── Init ──────────────────────────────────────────────────────────────────
    function init() {
        restoreState();
        bindEvents();
    }

    function restoreState() {
        currentPage = parseInt($('#currentPage').val()) || 1;
        currentSort = $('#currentSort').val() || '';
        currentSortDesc = $('#currentSortDesc').val() === 'true';
        currentSearch = $('#searchInput').val() || '';
        currentPageSize = parseInt($('#pageSizeSelect').val()) || 10;
        currentProjectId = $('#currentProjectId').val() || '';
        currentStatus = $('#currentStatus').val() || '';
    }

    // ── Bind Events ───────────────────────────────────────────────────────────
    function bindEvents() {

        // Search
        $(document).on('input', '#searchInput', function () {
            clearTimeout(searchTimer);
            searchTimer = setTimeout(function () {
                currentSearch = $('#searchInput').val().trim();
                currentPage = 1;
                loadList();
            }, 400);
        });

        // Project filter
        $(document).on('change', '#projectFilter', function () {
            currentProjectId = $(this).val();
            currentPage = 1;
            loadList();
        });

        // Status filter dropdown
        $(document).on('change', '#statusFilter', function () {
            currentStatus = $(this).val();
            currentPage = 1;
            loadList();
        });

        // Status badge click — quick filter
        $(document).on('click', '.status-badge', function () {
            const status = $(this).data('status');
            currentStatus = (currentStatus === status) ? '' : status;
            $('#statusFilter').val(currentStatus);
            currentPage = 1;
            loadList();
        });

        // Page size
        $(document).on('change', '#pageSizeSelect', function () {
            currentPageSize = parseInt($(this).val());
            currentPage = 1;
            loadList();
        });

        // Sort
        $(document).on('click', '.sortable', function () {
            const col = $(this).data('sort');
            currentSortDesc = currentSort === col ? !currentSortDesc : false;
            currentSort = col;
            currentPage = 1;
            loadList();
        });

        // Pagination
        $(document).on('click', '.task-page-btn', function (e) {
            e.preventDefault();
            const page = parseInt($(this).data('page'));
            if (!isNaN(page)) {
                currentPage = page;
                loadList();
            }
        });

        // Clear filters
        $(document).on('click', '#btnClearFilters', function () {
            currentSearch = '';
            currentProjectId = '';
            currentStatus = '';
            currentPage = 1;
            $('#searchInput').val('');
            $('#projectFilter').val('');
            $('#statusFilter').val('');
            loadList();
        });

        // Add task
        $(document).on('click', '#btnAddTask, #btnAddTaskEmpty', function () {
            const projectId = currentProjectId || '';
            loadForm(null, projectId);
        });

        // Edit task
        $(document).on('click', '.btn-edit-task', function () {
            const id = $(this).data('id');
            loadForm(id, null);
            const modal = bootstrap.Modal.getOrCreateInstance(
                document.getElementById('taskModal'));
            modal.show();
        });

        // Delete task
        $(document).on('click', '.btn-delete-task', function () {
            pendingDeleteId = $(this).data('id');
            $('#deleteTaskTitle').text($(this).data('title'));
            bootstrap.Modal.getOrCreateInstance(
                document.getElementById('deleteTaskModal')).show();
        });

        // Confirm delete
        $(document).on('click', '#btnConfirmDeleteTask', function () {
            if (pendingDeleteId) deleteTask(pendingDeleteId);
        });

        // Form submit
        $(document).on('submit', '#createTaskForm, #editTaskForm', function (e) {
            e.preventDefault();
            submitForm($(this));
        });

        // Quick status change (click badge in list)
        $(document).on('click', '.status-pill', function () {
            const taskId = $(this).data('task-id');
            const statuses = ['Pending', 'InProgress', 'Completed', 'OnHold', 'Cancelled'];
            const current = $(this).text().trim();
            const next = statuses[(statuses.indexOf(current) + 1) % statuses.length];
            quickUpdateStatus(taskId, next);
        });
    }

    // ── Load List ─────────────────────────────────────────────────────────────
    function loadList() {
        const params = $.param({
            page: currentPage,
            pageSize: currentPageSize,
            search: currentSearch,
            sortBy: currentSort,
            sortDesc: currentSortDesc,
            projectId: currentProjectId,
            statusFilter: currentStatus
        });

        $('#taskListContainer').css('opacity', '0.5');

        $.ajax({
            url: '/Task/Index?' + params,
            headers: { 'X-Requested-With': 'XMLHttpRequest' },
            success: function (html) {
                $('#taskListContainer').html(html).css('opacity', '1');
                restoreState();
            },
            error: function () {
                showToast('Failed to load tasks.', 'danger');
                $('#taskListContainer').css('opacity', '1');
            }
        });
    }

    // ── Load Form ─────────────────────────────────────────────────────────────
    function loadForm(id, projectId) {
        let url = id
            ? `/Task/GetForm?id=${id}`
            : `/Task/GetForm${projectId ? '?projectId=' + projectId : ''}`;

        $('#taskModalTitleText').text(id ? 'Edit Task' : 'New Task');
        $('#taskModalBody').html(
            '<div class="text-center py-4">' +
            '<div class="spinner-border text-primary" role="status"></div>' +
            '</div>');

        $.get(url, function (html) {
            $('#taskModalBody').html(html);
        }).fail(function () {
            showToast('Failed to load task form.', 'danger');
        });
    }

    // ── Submit Form ───────────────────────────────────────────────────────────
    function submitForm($form) {
        clearValidationErrors();

        const $btn = $('#btnSubmitTask');
        const $spinner = $('#taskSubmitSpinner');
        const $icon = $('#taskSubmitIcon');
        const action = window._taskFormAction;

        $btn.prop('disabled', true);
        $spinner.removeClass('d-none');
        $icon.addClass('d-none');

        $.ajax({
            url: action,
            type: 'POST',
            data: $form.serialize(),
            success: function (response) {
                if (response.success) {
                    bootstrap.Modal.getInstance(
                        document.getElementById('taskModal'))?.hide();
                    showToast(response.message, 'success');
                    currentPage = 1;
                    loadList();
                } else if (response.validationErrors) {
                    showValidationErrors(response.validationErrors);
                } else {
                    showToast(response.message || 'An error occurred.', 'danger');
                }
            },
            error: function () {
                showToast('Server error. Please try again.', 'danger');
            },
            complete: function () {
                $btn.prop('disabled', false);
                $spinner.addClass('d-none');
                $icon.removeClass('d-none');
            }
        });
    }

    // ── Delete ────────────────────────────────────────────────────────────────
    function deleteTask(id) {
        const $btn = $('#btnConfirmDeleteTask');
        $btn.prop('disabled', true).html(
            '<span class="spinner-border spinner-border-sm me-1"></span>Deleting...');

        $.ajax({
            url: '/Task/Delete',
            type: 'POST',
            data: { id, __RequestVerificationToken: getAntiForgeryToken() },
            success: function (response) {
                bootstrap.Modal.getInstance(
                    document.getElementById('deleteTaskModal'))?.hide();
                showToast(
                    response.success ? response.message : (response.message || 'Delete failed.'),
                    response.success ? 'success' : 'danger');
                if (response.success) loadList();
            },
            error: function () {
                showToast('Server error during delete.', 'danger');
            },
            complete: function () {
                $btn.prop('disabled', false)
                    .html('<i class="bi bi-trash me-1"></i>Delete');
                pendingDeleteId = null;
            }
        });
    }

    // ── Quick Status Change ───────────────────────────────────────────────────
    function quickUpdateStatus(taskId, status) {
        $.ajax({
            url: '/Task/UpdateStatus',
            type: 'POST',
            data: {
                id: taskId,
                status: status,
                __RequestVerificationToken: getAntiForgeryToken()
            },
            success: function (response) {
                if (response.success) {
                    showToast(response.message, 'success');
                    loadList();
                } else {
                    showToast(response.message, 'danger');
                }
            },
            error: function () {
                showToast('Failed to update status.', 'danger');
            }
        });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    function showToast(message, type) {
        const $toast = $('#toastNotification');
        $toast.removeClass('text-bg-success text-bg-danger text-bg-warning')
            .addClass(`text-bg-${type}`);
        $('#toastMessage').text(message);
        bootstrap.Toast.getOrCreateInstance(
            document.getElementById('toastNotification'),
            { delay: 4000 }).show();
    }

    function showValidationErrors(errors) {
        Object.entries(errors).forEach(([field, messages]) => {
            $(`[name="${field}"]`).addClass('is-invalid');
            const $fb = $(`[data-valmsg="${field}"]`);
            if ($fb.length) $fb.text(Array.isArray(messages) ? messages[0] : messages);
        });
    }

    function clearValidationErrors() {
        $('.is-invalid').removeClass('is-invalid');
        $('[data-valmsg]').text('');
    }

    function getAntiForgeryToken() {
        return $('input[name="__RequestVerificationToken"]').first().val();
    }

    return { init, loadList, loadForm };

})();

$(document).ready(function () {
    TaskModule.init();
});