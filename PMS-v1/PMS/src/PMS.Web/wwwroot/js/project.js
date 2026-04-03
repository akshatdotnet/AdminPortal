/**
 * project.js — Project module AJAX logic
 * Handles: list refresh, modal load, create, update, delete
 */

// ── State ─────────────────────────────────────────────────────────────────────
const ProjectModule = (function () {

    let currentPage = 1;
    let currentPageSize = 10;
    let currentSearch = '';
    let currentSort = '';
    let currentSortDesc = false;
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
        currentSortDesc = $('#currentDesc').val() === 'true';
        currentSearch = $('#searchInput').val() || '';
        currentPageSize = parseInt($('#pageSizeSelect').val()) || 10;
    }

    // ── Event Bindings ────────────────────────────────────────────────────────
    function bindEvents() {

        // Search — debounced 400ms
        $(document).on('input', '#searchInput', function () {
            clearTimeout(searchTimer);
            searchTimer = setTimeout(function () {
                currentSearch = $('#searchInput').val().trim();
                currentPage = 1;
                loadList();
            }, 400);
        });

        // Clear search
        $(document).on('click', '#btnClearSearch', function () {
            $('#searchInput').val('');
            currentSearch = '';
            currentPage = 1;
            loadList();
        });

        // Page size
        $(document).on('change', '#pageSizeSelect', function () {
            currentPageSize = parseInt($(this).val());
            currentPage = 1;
            loadList();
        });

        // Sorting
        $(document).on('click', '.sortable', function () {
            const col = $(this).data('sort');
            if (currentSort === col) {
                currentSortDesc = !currentSortDesc;
            } else {
                currentSort = col;
                currentSortDesc = false;
            }
            currentPage = 1;
            loadList();
        });

        // Pagination
        $(document).on('click', '.page-btn', function (e) {
            e.preventDefault();
            const page = parseInt($(this).data('page'));
            if (!isNaN(page)) {
                currentPage = page;
                loadList();
            }
        });

        // Add project — load empty form into modal
        $(document).on('click', '#btnAddProject, #btnAddProjectEmpty', function () {
            loadForm(null);
        });

        // Edit project
        $(document).on('click', '.btn-edit-project', function () {
            const id = $(this).data('id');
            loadForm(id);

            // Ensure modal opens if not already triggered by data-bs-toggle
            const modal = bootstrap.Modal.getOrCreateInstance(
                document.getElementById('projectModal'));
            modal.show();
        });

        // Delete — open confirm modal
        $(document).on('click', '.btn-delete-project', function () {
            pendingDeleteId = $(this).data('id');
            $('#deleteProjectName').text($(this).data('name'));
            bootstrap.Modal.getOrCreateInstance(
                document.getElementById('deleteModal')).show();
        });

        // Confirm delete
        $(document).on('click', '#btnConfirmDelete', function () {
            if (pendingDeleteId) deleteProject(pendingDeleteId);
        });

        // Form submit (delegated — form is injected dynamically)
        $(document).on('submit', '#createProjectForm, #editProjectForm', function (e) {
            e.preventDefault();
            submitForm($(this));
        });
    }

    // ── Load List (AJAX) ──────────────────────────────────────────────────────
    function loadList() {
        const params = $.param({
            page: currentPage,
            pageSize: currentPageSize,
            search: currentSearch,
            sortBy: currentSort,
            sortDesc: currentSortDesc
        });

        $('#projectListContainer').css('opacity', '0.5');

        $.ajax({
            url: '/Project/Index?' + params,
            headers: { 'X-Requested-With': 'XMLHttpRequest' },
            success: function (html) {
                $('#projectListContainer').html(html).css('opacity', '1');
                restoreState();
                updateRecordCount();
            },
            error: function () {
                showToast('Failed to load projects. Please try again.', 'danger');
                $('#projectListContainer').css('opacity', '1');
            }
        });
    }

    // ── Load Form into Modal ──────────────────────────────────────────────────
    function loadForm(id) {
        const url = id ? `/Project/GetForm?id=${id}` : '/Project/GetForm';

        $('#projectModalLabel .bi').removeClass('bi-folder-plus bi-pencil-square')
            .addClass(id ? 'bi-pencil-square' : 'bi-folder-plus');
        $('#modalTitleText').text(id ? 'Edit Project' : 'New Project');

        $('#projectModalBody').html(
            '<div class="text-center py-4">' +
            '<div class="spinner-border text-primary" role="status"></div>' +
            '</div>');

        $.get(url, function (html) {
            $('#projectModalBody').html(html);
        }).fail(function () {
            showToast('Failed to load form.', 'danger');
        });
    }

    // ── Submit Form (Create / Update) ─────────────────────────────────────────
    function submitForm($form) {
        clearValidationErrors();

        const $btn = $('#btnSubmitProject');
        const $spinner = $('#submitSpinner');
        const $icon = $('#submitIcon');
        const action = window._projectFormAction;

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
                        document.getElementById('projectModal'))?.hide();
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
    function deleteProject(id) {
        const $btn = $('#btnConfirmDelete');
        $btn.prop('disabled', true).html(
            '<span class="spinner-border spinner-border-sm me-1"></span>Deleting...');

        $.ajax({
            url: '/Project/Delete',
            type: 'POST',
            data: {
                id: id,
                __RequestVerificationToken: getAntiForgeryToken()
            },
            success: function (response) {
                bootstrap.Modal.getInstance(
                    document.getElementById('deleteModal'))?.hide();

                if (response.success) {
                    showToast(response.message, 'success');
                    loadList();
                } else {
                    showToast(response.message || 'Delete failed.', 'danger');
                }
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

    // ── Helpers ───────────────────────────────────────────────────────────────
    function showToast(message, type) {
        const $toast = $('#toastNotification');
        $toast.removeClass('text-bg-success text-bg-danger text-bg-warning')
            .addClass(`text-bg-${type}`);
        $('#toastMessage').text(message);

        const toast = bootstrap.Toast.getOrCreateInstance(
            document.getElementById('toastNotification'),
            { delay: 4000 });
        toast.show();
    }

    function showValidationErrors(errors) {
        Object.entries(errors).forEach(([field, messages]) => {
            const $input = $(`[name="${field}"]`);
            $input.addClass('is-invalid');
            const $feedback = $(`[data-valmsg="${field}"]`);
            if ($feedback.length) {
                $feedback.text(Array.isArray(messages) ? messages[0] : messages);
            }
        });
    }

    function clearValidationErrors() {
        $('.is-invalid').removeClass('is-invalid');
        $('[data-valmsg]').text('');
    }

    function updateRecordCount() {
        const total = parseInt($('#totalCount').val()) || 0;
        const items = $('#projectTable tbody tr').length;
        $('#recordCount').text(items);
    }

    function getAntiForgeryToken() {
        return $('input[name="__RequestVerificationToken"]').first().val();
    }

    return { init };

})();

// ── Bootstrap on DOM ready ────────────────────────────────────────────────────
$(document).ready(function () {
    ProjectModule.init();
});