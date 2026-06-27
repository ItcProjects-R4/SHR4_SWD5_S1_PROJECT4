// Script for Custom Confirm Modal and Admin Layout Sidebar behavior

(function() {
    const modal = document.getElementById('customConfirmModal');
    if (modal) {
        const messageEl = document.getElementById('customConfirmMessage');
        const btnCancel = document.getElementById('btnConfirmCancel');
        const btnYes = document.getElementById('btnConfirmYes');
        const iconDiv = modal.querySelector('.confirm-modal-icon');
        const iconElement = iconDiv ? iconDiv.querySelector('i') : null;
        
        let onConfirmCallback = null;
        let activeForm = null;

        // Global function to show custom confirm
        window.showCustomConfirm = function(message, callback) {
            if (messageEl) messageEl.textContent = message;
            onConfirmCallback = callback;

            // Destructive / logout styling
            const isDestructive = /delete|remove|cancel|permanently|clear|lock|block/i.test(message.toLowerCase())
                || message.includes('حذف')
                || message.includes('الغاء')
                || message.includes('حظر')
                || message.includes('فك');
            
            if (isDestructive) {
                if (iconDiv) iconDiv.style.background = 'linear-gradient(135deg, #ef4444, #dc2626)';
                if (iconDiv) iconDiv.style.boxShadow = '0 8px 20px rgba(239, 68, 68, 0.3)';
                if (iconElement) iconElement.className = 'fas fa-exclamation-triangle';
                if (btnYes) btnYes.style.background = 'linear-gradient(135deg, #ef4444, #dc2626)';
                if (btnYes) btnYes.style.boxShadow = '0 4px 12px rgba(239, 68, 68, 0.2)';
                if (btnYes) btnYes.textContent = 'Yes, Proceed';
            } else if (/log out|logout|خروج/i.test(message.toLowerCase())) {
                if (iconDiv) iconDiv.style.background = 'linear-gradient(135deg, #f59e0b, #d97706)';
                if (iconDiv) iconDiv.style.boxShadow = '0 8px 20px rgba(245, 158, 11, 0.3)';
                if (iconElement) iconElement.className = 'fas fa-sign-out-alt';
                if (btnYes) btnYes.style.background = 'linear-gradient(135deg, #f59e0b, #d97706)';
                if (btnYes) btnYes.style.boxShadow = '0 4px 12px rgba(245, 158, 11, 0.2)';
                if (btnYes) btnYes.textContent = 'Yes, Logout';
            } else {
                if (iconDiv) iconDiv.style.background = 'linear-gradient(135deg, #7c3aed, #5b21b6)';
                if (iconDiv) iconDiv.style.boxShadow = '0 8px 20px rgba(124, 58, 237, 0.3)';
                if (iconElement) iconElement.className = 'fas fa-question';
                if (btnYes) btnYes.style.background = 'linear-gradient(135deg, #7c3aed, #5b21b6)';
                if (btnYes) btnYes.style.boxShadow = '0 4px 12px rgba(124, 58, 237, 0.2)';
                if (btnYes) btnYes.textContent = 'Yes, Proceed';
            }

            // Show modal
            modal.classList.add('active');
            if (btnCancel) btnCancel.focus();
        };

        // Close modal helper
        function closeModal() {
            modal.classList.remove('active');
            onConfirmCallback = null;
            activeForm = null;
        }

        // Click event handlers
        if (btnCancel) btnCancel.addEventListener('click', closeModal);
        if (btnYes) btnYes.addEventListener('click', function() {
            if (onConfirmCallback) {
                onConfirmCallback();
            }
            closeModal();
        });

        // Keyboard support (Escape to close)
        window.addEventListener('keydown', function(e) {
            if (modal.classList.contains('active')) {
                if (e.key === 'Escape') {
                    closeModal();
                }
            }
        });

        // Catch form submissions that contain inline confirmation
        document.addEventListener("submit", function(e) {
            const form = e.target;
            if (form.dataset.confirmed === "true") {
                return;
            }

            const onsubmitAttr = form.getAttribute("onsubmit");
            if (onsubmitAttr && onsubmitAttr.includes("confirm(")) {
                e.preventDefault();
                e.stopPropagation();
                activeForm = form;

                // Extract message
                let message = "Are you sure you want to proceed?";
                const match = onsubmitAttr.match(/confirm\s*\(\s*(['"`])(.*?)\1\s*\)/);
                if (match && match[2]) {
                    message = match[2].replace(/\\'/g, "'").replace(/\\"/g, '"');
                }

                window.showCustomConfirm(message, function() {
                    activeForm.dataset.confirmed = "true";
                    activeForm.submit();
                });
            }
        }, true);

        // Hook for custom JS confirmation (e.g. bulk actions)
        window.confirmAction = function(message, callback) {
            window.showCustomConfirm(message, callback);
        };
    }
})();

// Layout sidebar behaviors
document.addEventListener("DOMContentLoaded", function() {
    // Check for collapsed state in localStorage on page load
    if (localStorage.getItem('sidebar-collapsed') === 'true') {
        document.querySelector('.dashboard-layout')?.classList.add('collapsed');
    }

    // Mobile toggle
    document.getElementById('sidebarToggle')?.addEventListener('click', function() {
        document.querySelector('.sidebar')?.classList.add('active');
        document.getElementById('sidebarOverlay')?.classList.add('active');
    });

    const closeSidebar = function() {
        document.querySelector('.sidebar')?.classList.remove('active');
        document.getElementById('sidebarOverlay')?.classList.remove('active');
    };

    document.getElementById('sidebarClose')?.addEventListener('click', closeSidebar);
    document.getElementById('sidebarOverlay')?.addEventListener('click', closeSidebar);

    // Desktop collapse toggle
    document.getElementById('sidebarCollapse')?.addEventListener('click', function() {
        const layout = document.querySelector('.dashboard-layout');
        if (layout) {
            const isCollapsed = layout.classList.toggle('collapsed');
            localStorage.setItem('sidebar-collapsed', isCollapsed);
        }
    });
});
