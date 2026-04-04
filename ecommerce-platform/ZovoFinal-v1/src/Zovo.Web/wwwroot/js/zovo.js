/* ============================================================
   ZOVO Admin — zovo.js  v1.0
   Handles: alerts, dropdown menus, AJAX toggle/delete/order-status
============================================================ */

// ── Toast notifications ──────────────────────────────────────
function showToast(type, message) {
    document.querySelectorAll('.alert[data-auto]').forEach(e => e.remove());
    const el = document.createElement('div');
    el.className = `alert alert-${type}`;
    el.setAttribute('data-auto', '1');
    el.innerHTML = `<span>${type === 'success' ? '✓' : '⚠'}</span>
        <span>${escHtml(message)}</span>
        <button class="alert-dismiss" onclick="this.parentElement.remove()">✕</button>`;
    const body = document.querySelector('.page-body');
    if (body) body.prepend(el);
    setTimeout(() => { el.style.transition = 'opacity .3s'; el.style.opacity = '0'; setTimeout(() => el.remove(), 300); }, 4000);
}

function escHtml(str) {
    return str.replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;');
}

// ── Auto-dismiss server-side alerts ─────────────────────────
document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('.alert:not([data-auto])').forEach(el => {
        setTimeout(() => { el.style.transition='opacity .3s'; el.style.opacity='0'; setTimeout(()=>el.remove(),300); }, 4000);
    });
    // Apply auto-filter listeners
    document.querySelectorAll('.auto-filter').forEach(el =>
        el.addEventListener('change', () => document.getElementById('filterForm')?.submit()));
    // Debounced search
    const searchInput = document.getElementById('filterSearch');
    if (searchInput) {
        let timer;
        searchInput.addEventListener('input', () => {
            clearTimeout(timer);
            timer = setTimeout(() => document.getElementById('filterForm')?.submit(), 450);
        });
    }
});

// ── Dropdown menus ───────────────────────────────────────────
document.addEventListener('click', (e) => {
    const toggle = e.target.closest('[data-dd]');
    document.querySelectorAll('.dropdown.open').forEach(d => {
        if (!d.previousElementSibling?.contains(e.target)) d.classList.remove('open');
    });
    if (toggle) { e.stopPropagation(); toggle.nextElementSibling?.classList.toggle('open'); }
});

// ── CSRF token ───────────────────────────────────────────────
function csrf() {
    return document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';
}

async function postForm(url, params = {}) {
    const body = Object.entries(params).map(([k, v]) => `${encodeURIComponent(k)}=${encodeURIComponent(v)}`).join('&');
    const res = await fetch(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded', 'RequestVerificationToken': csrf() },
        body
    });
    return res.json();
}

// ── Product status toggle ─────────────────────────────────────
async function toggleProduct(id, cb) {
    const prev = cb.checked;
    try {
        const d = await postForm(`/Products/Toggle/${id}`);
        if (d.success) showToast('success', d.message);
        else { showToast('error', d.message); cb.checked = !prev; }
    } catch { showToast('error', 'Network error. Please retry.'); cb.checked = !prev; }
}

// ── Customer status toggle ───────────────────────────────────
async function toggleCustomer(id) {
    try {
        const d = await postForm(`/Customers/Toggle/${id}`);
        if (d.success) { showToast('success', d.message); setTimeout(() => location.reload(), 800); }
        else showToast('error', d.message);
    } catch { showToast('error', 'Network error.'); }
}

// ── Order status update ──────────────────────────────────────
async function updateOrderStatus(id, status) {
    try {
        const d = await postForm('/Orders/UpdateStatus', { id, status });
        if (d.success) { showToast('success', d.message); setTimeout(() => location.reload(), 700); }
        else showToast('error', d.message);
    } catch { showToast('error', 'Network error.'); }
}

// ── Delete confirm modal ─────────────────────────────────────
let _delId = null, _delUrl = null;

function openDeleteModal(id, name, url) {
    _delId = id; _delUrl = url;
    const el = document.getElementById('deleteTarget');
    if (el) el.textContent = name;
    document.getElementById('deleteModal')?.classList.add('open');
}

function closeDeleteModal() {
    _delId = null; _delUrl = null;
    document.getElementById('deleteModal')?.classList.remove('open');
}

async function confirmDelete() {
    if (!_delId) return;
    const btn = document.getElementById('confirmDeleteBtn');
    if (btn) { btn.disabled = true; btn.innerHTML = '<span class="spinner"></span>'; }
    try {
        const d = await postForm(`${_delUrl}/${_delId}`, { id: _delId });
        closeDeleteModal();
        if (d.success) {
            const row = document.querySelector(`tr[data-id="${_delId}"]`);
            if (row) { row.style.transition = 'all .3s'; row.style.opacity = '0'; row.style.transform = 'translateX(20px)'; setTimeout(() => row.remove(), 300); }
            showToast('success', d.message);
        } else showToast('error', d.message);
    } catch { showToast('error', 'Delete failed. Please try again.'); closeDeleteModal(); }
    finally { if (btn) { btn.disabled = false; btn.textContent = 'Delete'; } }
}

// Close modal clicking backdrop
document.addEventListener('click', e => { if (e.target.id === 'deleteModal') closeDeleteModal(); });
