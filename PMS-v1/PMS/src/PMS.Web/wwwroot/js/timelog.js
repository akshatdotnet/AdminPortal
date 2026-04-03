/**
 * timelog.js — Time tracking module
 * Handles: start/stop timer, live elapsed display, log history, delete log
 */

const TimeLogModule = (function () {

    let _taskId = null;
    let _liveInterval = null;   // setInterval handle for live display
    let _startTime = null;   // Date object when timer started

    // ── Public: Load Summary Partial ──────────────────────────────────────────
    function loadSummary(taskId) {
        _taskId = taskId;

        $('#timeLogContainer').html(
            '<div class="text-center py-3 text-muted">' +
            '<div class="spinner-border spinner-border-sm me-2"></div>' +
            'Loading time logs...</div>');

        $.get(`/TimeLog/Summary?taskId=${taskId}`, function (html) {
            $('#timeLogContainer').html(html);
            initAfterLoad();
        }).fail(function () {
            $('#timeLogContainer').html(
                '<div class="alert alert-danger mb-0">' +
                'Failed to load time logs.</div>');
        });
    }

    // ── Init after partial is injected ────────────────────────────────────────
    function initAfterLoad() {
        _taskId = parseInt($('#summaryTaskId').val());
        const hasActive = $('#summaryHasActive').val() === 'true';

        bindEvents();

        if (hasActive) {
            const $liveDisplay = $('#liveTimerDisplay');
            if ($liveDisplay.length) {
                const startIso = $liveDisplay.data('start-time');
                if (startIso) startLiveTick(new Date(startIso));
            }
            // Also update live-duration spans in table rows
            startLiveDurationTick();
        }
    }

    // ── Bind Events ───────────────────────────────────────────────────────────
    function bindEvents() {

        // Start timer button (on Detail page header or in summary)
        $(document).off('click', '#btnStartTimer')
            .on('click', '#btnStartTimer', function () {
                const notes = $('#timerNotes').val().trim();
                startTimer(_taskId, notes);
            });

        // Stop timer button
        $(document).off('click', '#btnStopTimer')
            .on('click', '#btnStopTimer', function () {
                stopTimer(_taskId);
            });

        // Delete log button
        $(document).off('click', '.btn-delete-log')
            .on('click', '.btn-delete-log', function () {
                const logId = $(this).data('log-id');
                const taskId = $(this).data('task-id');
                confirmDeleteLog(logId, taskId);
            });
    }

    // ── Start Timer ───────────────────────────────────────────────────────────
    function startTimer(taskId, notes) {
        const $btn = $('#btnStartTimer');
        $btn.prop('disabled', true)
            .html('<span class="spinner-border spinner-border-sm me-1"></span>Starting...');

        $.ajax({
            url: '/TimeLog/Start',
            type: 'POST',
            data: {
                taskId,
                notes,
                __RequestVerificationToken: getAntiForgeryToken()
            },
            success: function (response) {
                if (response.success) {
                    showToast('Timer started!', 'success');
                    _startTime = new Date(response.startTime);
                    // Reload summary to show running state
                    loadSummary(taskId);
                    // Refresh header button on Task Detail page
                    refreshDetailTimerButtons(taskId, true);
                } else {
                    showToast(response.message || 'Failed to start timer.', 'danger');
                    $btn.prop('disabled', false)
                        .html('<i class="bi bi-play-circle me-1"></i>Start Timer');
                }
            },
            error: function () {
                showToast('Server error starting timer.', 'danger');
                $btn.prop('disabled', false)
                    .html('<i class="bi bi-play-circle me-1"></i>Start Timer');
            }
        });
    }

    // ── Stop Timer ────────────────────────────────────────────────────────────
    function stopTimer(taskId) {
        const $btn = $('#btnStopTimer');
        $btn.prop('disabled', true)
            .html('<span class="spinner-border spinner-border-sm me-1"></span>Stopping...');

        $.ajax({
            url: '/TimeLog/Stop',
            type: 'POST',
            data: {
                taskId,
                __RequestVerificationToken: getAntiForgeryToken()
            },
            success: function (response) {
                if (response.success) {
                    stopLiveTick();
                    showToast(
                        `Timer stopped. Duration: ${response.formattedDuration}`,
                        'success');
                    loadSummary(taskId);
                    refreshDetailTimerButtons(taskId, false);
                } else {
                    showToast(response.message || 'Failed to stop timer.', 'danger');
                    $btn.prop('disabled', false)
                        .html('<i class="bi bi-stop-circle me-1"></i>Stop Timer');
                }
            },
            error: function () {
                showToast('Server error stopping timer.', 'danger');
                $btn.prop('disabled', false)
                    .html('<i class="bi bi-stop-circle me-1"></i>Stop Timer');
            }
        });
    }

    // ── Delete Log ────────────────────────────────────────────────────────────
    function confirmDeleteLog(logId, taskId) {
        if (!confirm('Delete this time log entry? This cannot be undone.')) return;

        $.ajax({
            url: '/TimeLog/Delete',
            type: 'POST',
            data: {
                logId,
                taskId,
                __RequestVerificationToken: getAntiForgeryToken()
            },
            success: function (response) {
                if (response.success) {
                    showToast('Time log deleted.', 'success');
                    // Remove row with fade animation
                    $(`tr[data-log-id="${logId}"]`)
                        .fadeOut(300, function () {
                            $(this).remove();
                            recalculateTotalsFromDom();
                        });
                } else {
                    showToast(response.message || 'Failed to delete log.', 'danger');
                }
            },
            error: function () {
                showToast('Server error deleting log.', 'danger');
            }
        });
    }

    // ── Live Tick — Header Display ────────────────────────────────────────────
    function startLiveTick(startTime) {
        _startTime = startTime;
        stopLiveTick(); // clear any existing interval

        _liveInterval = setInterval(function () {
            const elapsed = Math.floor((Date.now() - _startTime.getTime()) / 1000);
            const hours = Math.floor(elapsed / 3600);
            const minutes = Math.floor((elapsed % 3600) / 60);
            const seconds = elapsed % 60;
            const display = [
                String(hours).padStart(2, '0'),
                String(minutes).padStart(2, '0'),
                String(seconds).padStart(2, '0')
            ].join(':');

            $('#liveTimerDisplay').text(display);
        }, 1000);
    }

    function stopLiveTick() {
        if (_liveInterval) {
            clearInterval(_liveInterval);
            _liveInterval = null;
        }
    }

    // ── Live Tick — Table Row Duration ────────────────────────────────────────
    function startLiveDurationTick() {
        // Update all .live-duration spans every second
        setInterval(function () {
            $('.live-duration').each(function () {
                const startIso = $(this).data('start');
                if (!startIso) return;

                const start = new Date(startIso);
                const elapsed = Math.floor((Date.now() - start.getTime()) / 1000);
                const hours = Math.floor(elapsed / 3600);
                const minutes = Math.floor((elapsed % 3600) / 60);
                const seconds = elapsed % 60;

                let display;
                if (hours > 0) {
                    display = `${hours}h ${String(minutes).padStart(2, '0')}m ` +
                        `${String(seconds).padStart(2, '0')}s`;
                } else if (minutes > 0) {
                    display = `${minutes}m ${String(seconds).padStart(2, '0')}s`;
                } else {
                    display = `${seconds}s`;
                }

                $(this).text(display);
            });
        }, 1000);
    }

    // ── Recalculate totals from remaining DOM rows ─────────────────────────────
    function recalculateTotalsFromDom() {
        let totalRows = $('#timeLogTable tbody tr').length;

        // Update row numbers
        $('#timeLogTable tbody tr').each(function (i) {
            $(this).find('td:first').text(totalRows - i);
        });

        if (totalRows === 0) {
            $('#timeLogTableBody').closest('.table-responsive')
                .replaceWith(
                    '<div class="text-center py-4 text-muted border rounded mt-3">' +
                    '<i class="bi bi-clock-history display-5 mb-3 d-block"></i>' +
                    'No time logs yet. Start the timer to begin tracking.' +
                    '</div>');
        }

        // Reload full summary to get accurate totals from server
        setTimeout(() => loadSummary(_taskId), 600);
    }

    // ── Refresh Timer Buttons on Task Detail page header ─────────────────────
    function refreshDetailTimerButtons(taskId, isRunning) {
        const $container = $('#timelog .card-header .d-flex.gap-2');
        if (!$container.length) return;

        if (isRunning) {
            $container.html(
                `<button class="btn btn-sm btn-danger" id="btnStopTimer"
                         data-task-id="${taskId}">
                    <i class="bi bi-stop-circle me-1"></i>Stop Timer
                 </button>`);
        } else {
            $container.html(
                `<button class="btn btn-sm btn-success" id="btnStartTimer"
                         data-task-id="${taskId}">
                    <i class="bi bi-play-circle me-1"></i>Start Timer
                 </button>`);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    function showToast(message, type) {
        const el = document.getElementById('toastNotification');
        if (!el) return;

        const $toast = $(el);
        $toast.removeClass('text-bg-success text-bg-danger text-bg-warning')
            .addClass(`text-bg-${type}`);
        $('#toastMessage').text(message);

        bootstrap.Toast.getOrCreateInstance(el, { delay: 4000 }).show();
    }

    function getAntiForgeryToken() {
        return $('input[name="__RequestVerificationToken"]').first().val();
    }

    // ── Public API ────────────────────────────────────────────────────────────
    return {
        loadSummary,
        startTimer,
        stopTimer
    };

})();