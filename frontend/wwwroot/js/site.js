// ==========================================
// Aumo Finance - Global Site JavaScript
// Cosmic / Nebula Theme
// ==========================================


// ==========================================
// Bootstrap Modal Helper
// ==========================================
//
// Helper ini digunakan oleh komponen Blazor
// untuk membuka dan menutup Bootstrap Modal
// secara terprogram tanpa reload halaman.
//

window.aumoModal = {

    /**
     * Show Bootstrap modal.
     *
     * @param {string} elementId
     */
    show: function (elementId) {
        const element = document.getElementById(elementId);

        if (!element) {
            console.warn(`[aumoModal] Element with ID '${elementId}' not found.`);
            return;
        }

        if (typeof bootstrap === 'undefined') {
            console.error('[aumoModal] Bootstrap JS library is not loaded.');
            return;
        }

        const instance = bootstrap.Modal.getOrCreateInstance(element);
        instance.show();
    },

    /**
     * Hide Bootstrap modal.
     *
     * @param {string} elementId
     */
    hide: function (elementId) {
        const element = document.getElementById(elementId);

        if (!element) {
            console.warn(`[aumoModal] Element with ID '${elementId}' not found.`);
            return;
        }

        if (typeof bootstrap === 'undefined') {
            console.error('[aumoModal] Bootstrap JS library is not loaded.');
            return;
        }

        const instance = bootstrap.Modal.getInstance(element) || bootstrap.Modal.getOrCreateInstance(element);
        if (instance) {
            instance.hide();
        }

        // Cleanup tambahan untuk memastikan backdrop gelap terhapus jika tersisa
        setTimeout(() => {
            const backdrops = document.querySelectorAll('.modal-backdrop');
            if (backdrops.length > 0 && !document.querySelector('.modal.show')) {
                backdrops.forEach(b => b.remove());
                document.body.classList.remove('modal-open');
                document.body.style.removeProperty('overflow');
                document.body.style.removeProperty('padding-right');
            }
        }, 150);
    }
};


// ==========================================
// Aumo Finance Theme Controller
// ==========================================
//
// Theme values:
//   dark  = Cosmic / Nebula Dark
//   light = Aumo Light
//
// Dark is always the fallback/default.
//

window.aumoTheme = {

    /**
     * Get the currently active theme.
     *
     * @returns {string}
     */
    get: function () {
        const currentTheme = document.documentElement.getAttribute('data-bs-theme');
        return currentTheme === 'light' ? 'light' : 'dark';
    },

    /**
     * Apply a theme.
     *
     * @param {string} themeName
     */
    set: function (themeName) {
        const theme = themeName === 'light' ? 'light' : 'dark';

        document.documentElement.setAttribute('data-bs-theme', theme);
        localStorage.setItem('aumo_theme', theme);
    },

    /**
     * Toggle between dark and light theme.
     *
     * @returns {string}
     */
    toggle: function () {
        const currentTheme = this.get();
        const nextTheme = currentTheme === 'dark' ? 'light' : 'dark';

        this.set(nextTheme);
        return nextTheme;
    },

    /**
     * Restore the saved theme.
     *
     * Dark is used when no valid preference exists.
     *
     * @returns {string}
     */
    restore: function () {
        const savedTheme = localStorage.getItem('aumo_theme');
        const theme = (savedTheme === 'light' || savedTheme === 'dark') ? savedTheme : 'dark';

        document.documentElement.setAttribute('data-bs-theme', theme);
        return theme;
    }
};


// ==========================================
// Device Local Timestamp Helper
// ==========================================
//
// Dipakai untuk mencatat CreatedAt jurnal berdasarkan waktu
// lokal PERANGKAT saat input, bukan waktu server saat data
// disimpan ke database.
//

window.aumoTime = {

    /**
     * Ambil waktu lokal perangkat saat ini sebagai string
     * "YYYY-MM-DDTHH:mm:ss" (tanpa konversi timezone apapun,
     * murni jam dinding perangkat).
     */
    getLocalTimestamp: function () {
        const d = new Date();
        const pad = (n) => String(n).padStart(2, '0');
        return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}:${pad(d.getSeconds())}`;
    }
};


// ==========================================
// Initial Theme Restoration
// ==========================================

(function () {
    try {
        if (window.aumoTheme) {
            window.aumoTheme.restore();
        }
    } catch (error) {
        console.warn('Aumo Finance: Unable to restore theme.', error);
        document.documentElement.setAttribute('data-bs-theme', 'dark');
    }
})();


// Unduh berkas dari byte base64 (dipakai halaman Tools untuk template Excel).
window.aumoDownload = function (fileName, contentType, base64) {
    const link = document.createElement('a');
    link.href = 'data:' + contentType + ';base64,' + base64;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
};


// ==========================================
// Sidebar Reports Flyout (lipat / buka)
// ==========================================
//
// Murni JavaScript + delegasi event pada document, sehingga tetap
// berfungsi walau DOM dirender ulang oleh Blazor dan tidak bergantung
// pada koneksi sirkuit Blazor Server.
//

window.aumoSidebar = {
    set: function (open) {
        const wrapper = document.querySelector('.sidebar-wrapper');
        if (!wrapper) return;

        wrapper.classList.toggle('flyout-open', open);

        const toggle = wrapper.querySelector('[data-flyout-toggle]');
        if (toggle) {
            toggle.classList.toggle('active', open);
            toggle.setAttribute('aria-expanded', open ? 'true' : 'false');
        }
    },

    isOpen: function () {
        const wrapper = document.querySelector('.sidebar-wrapper');
        return !!wrapper && wrapper.classList.contains('flyout-open');
    }
};

document.addEventListener('click', function (event) {
    const target = event.target;
    if (!(target instanceof Element)) return;

    if (target.closest('[data-flyout-toggle]')) {
        window.aumoSidebar.set(!window.aumoSidebar.isOpen());
        return;
    }

    if (!window.aumoSidebar.isOpen()) return;

    if (target.closest('[data-flyout-close]') ||
        target.closest('.sidebar-backdrop') ||
        target.closest('.sidebar-flyout a') ||
        target.closest('.sidebar-icons a')) {
        window.aumoSidebar.set(false);
    }
});

document.addEventListener('keydown', function (event) {
    if (event.key === 'Escape') {
        window.aumoSidebar.set(false);
    }
});
