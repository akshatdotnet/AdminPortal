/**
 * site.js — Global PMS utilities
 * Runs on every page
 */

// ── Sidebar Toggle (mobile) ───────────────────────────────────────────────────
$(document).ready(function () {

    const $sidebar = $('#pmsSidebar');
    const $overlay = $('#sidebarOverlay');
    const $mainToggle = $('#mobileSidebarToggle');

    function openSidebar() {
        $sidebar.addClass('show');
        $overlay.addClass('show');
        document.body.style.overflow = 'hidden';
    }

    function closeSidebar() {
        $sidebar.removeClass('show');
        $overlay.removeClass('show');
        document.body.style.overflow = '';
    }

    $mainToggle.on('click', openSidebar);
    $('#sidebarToggle').on('click', closeSidebar);
    $overlay.on('click', closeSidebar);

    // Close sidebar on navigation (mobile)
    $sidebar.find('a').on('click', function () {
        if (window.innerWidth < 992) closeSidebar();
    });

    // ── Global Toast Helper ───────────────────────────────────────────────────
    window.PMS = window.PMS || {};

    window.PMS.showToast = function (message, type = 'success') {
        const iconMap = {
            success: 'bi-check-circle-fill text-white',
            danger: 'bi-x-circle-fill text-white',
            warning: 'bi-exclamation-triangle-fill text-dark',
            info: 'bi-info-circle-fill text-white'
        };

        const $toast = $('#toastNotification');
        $toast.removeClass(
            'text-bg-success text-bg-danger text-bg-warning text-bg-info')
            .addClass(`text-bg-${type}`);

        const btnClose = $toast.find('.btn-close');
        btnClose.removeClass('btn-close-white');
        if (type !== 'warning') btnClose.addClass('btn-close-white');

        $('#toastMessage').text(message);
        $toast.find('.toast-icon')
            .attr('class', `bi toast-icon fs-5 ${iconMap[type] || ''}`);

        bootstrap.Toast.getOrCreateInstance(
            document.getElementById('toastNotification'),
            { delay: 4000 }
        ).show();
    };

    // ── Anti-forgery Token Helper ─────────────────────────────────────────────
    window.PMS.getToken = function () {
        return $('input[name="__RequestVerificationToken"]').first().val();
    };

    // ── Skeleton Loader Helpers ───────────────────────────────────────────────
    window.PMS.showSkeleton = function (containerId, rows = 5) {
        const skeletonRows = Array.from({ length: rows }, () =>
            '<div class="skeleton skeleton-row mb-2"></div>'
        ).join('');

        $(`#${containerId}`).html(`
            <div class="p-3">
                <div class="skeleton skeleton-title mb-3"></div>
                ${skeletonRows}
            </div>`);
    };

    // ── Global AJAX error handler ─────────────────────────────────────────────
    $(document).ajaxError(function (event, xhr) {
        if (xhr.status === 401) {
            window.PMS.showToast('Session expired. Please refresh.', 'warning');
        } else if (xhr.status === 403) {
            window.PMS.showToast('You do not have permission.', 'danger');
        } else if (xhr.status === 404) {
            // Suppress — handled per-call
        } else if (xhr.status >= 500) {
            window.PMS.showToast('A server error occurred. Please try again.', 'danger');
        }
    });

    // ── Quick Add Project (sidebar modal) ─────────────────────────────────────
    $('#quickAddModal').on('show.bs.modal', function () {
        $('#quickAddModalBody').html(
            '<div class="text-center py-4">' +
            '<div class="spinner-border text-primary" role="status"></div>' +
            '</div>');

        $.get('/Project/GetForm', function (html) {
            $('#quickAddModalBody').html(html);
        });
    });

    $(document).on('submit', '#createProjectForm', function (e) {
        const isInQuickModal = $(this).closest('#quickAddModal').length;
        if (!isInQuickModal) return;

        e.preventDefault();
        e.stopPropagation();

        $.ajax({
            url: '/Project/Create',
            type: 'POST',
            data: $(this).serialize(),
            success: function (response) {
                if (response.success) {
                    bootstrap.Modal.getInstance(
                        document.getElementById('quickAddModal'))?.hide();
                    window.PMS.showToast(response.message, 'success');
                    if (window.location.pathname.includes('/Project'))
                        window.location.reload();
                } else {
                    window.PMS.showToast(
                        response.message || 'Validation failed.', 'warning');
                }
            }
        });
    });

    // ── Sidebar Quick Add Task ────────────────────────────────────────────────
    $('#sidebarAddTask').on('click', function (e) {
        e.preventDefault();
        window.location.href = '/Task/Index';
    });

    // ── Confirm dialog helper ─────────────────────────────────────────────────
    window.PMS.confirm = function (message) {
        return window.confirm(message);
    };

    // ── Format elapsed seconds ────────────────────────────────────────────────
    window.PMS.formatElapsed = function (totalSeconds) {
        const h = Math.floor(totalSeconds / 3600);
        const m = Math.floor((totalSeconds % 3600) / 60);
        const s = totalSeconds % 60;
        if (h > 0) return `${h}h ${String(m).padStart(2, '0')}m`;
        if (m > 0) return `${m}m ${String(s).padStart(2, '0')}s`;
        return `${s}s`;
    };

});