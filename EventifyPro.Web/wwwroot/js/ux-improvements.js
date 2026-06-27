/**
 * Eventify Pro - Quick-Win UX Improvements & Micro-Interactions Script
 */

(function () {
    document.addEventListener("DOMContentLoaded", function () {
        
        // ==========================================
        // 1. Top Page Load Progress Bar Inject & Init
        // ==========================================
        const bar = document.createElement('div');
        bar.id = 'top-loading-bar';
        document.body.appendChild(bar);

        // Animate initial load to 100% and fade out
        setTimeout(function () {
            bar.style.width = '100%';
            setTimeout(function () {
                bar.style.opacity = '0';
                setTimeout(function () {
                    bar.style.width = '0%';
                }, 300);
            }, 180);
        }, 50);

        // Beforeunload hook to fill bar on navigating away
        window.addEventListener('beforeunload', function () {
            bar.style.transition = 'width 1.2s cubic-bezier(0.16, 1, 0.3, 1)';
            bar.style.opacity = '1';
            bar.style.width = '100%';
        });

        // Trigger partial progress bar on click of local links to feel responsive
        document.addEventListener('click', function (e) {
            const link = e.target.closest('a');
            if (link && 
                link.href && 
                !link.href.startsWith('#') && 
                !link.href.startsWith('javascript:') && 
                !link.target && 
                link.getAttribute('download') === null &&
                link.hostname === window.location.hostname) {
                
                bar.style.transition = 'width 0.4s ease';
                bar.style.opacity = '1';
                bar.style.width = '70%';
            }
        });

        // ==========================================
        // 2. Back to Top Button Inject & Smooth Scroll
        // ==========================================
        const backToTopBtn = document.createElement('button');
        backToTopBtn.className = 'back-to-top-btn';
        backToTopBtn.setAttribute('aria-label', 'Back to top');
        backToTopBtn.innerHTML = '<i class="fa-solid fa-arrow-up"></i>';
        document.body.appendChild(backToTopBtn);

        // Scroll listener for Back to Top visibility
        window.addEventListener('scroll', function () {
            if (window.scrollY > 300) {
                backToTopBtn.classList.add('show');
            } else {
                backToTopBtn.classList.remove('show');
            }
        });

        // Click handler to scroll back smoothly
        backToTopBtn.addEventListener('click', function () {
            window.scrollTo({
                top: 0,
                behavior: 'smooth'
            });
        });

        // ==========================================
        // 3. Automated Button Loading Spinners for Forms
        // ==========================================
        document.addEventListener('submit', function (e) {
            const form = e.target;
            
            // Skip form validation failure
            // Check if HTML5 Validation is supported and the form is invalid
            if (typeof form.checkValidity === 'function' && !form.checkValidity()) {
                return;
            }

            // Skip if custom 'data-no-spinner' attribute is present
            if (form.getAttribute('data-no-spinner') !== null) {
                return;
            }

            // Find the primary submit button in this form
            const submitBtn = form.querySelector('button[type="submit"], input[type="submit"]');
            if (submitBtn) {
                if (submitBtn.tagName.toLowerCase() === 'button') {
                    // Set explicit sizes to prevent the button from collapsing/shrinking
                    const rect = submitBtn.getBoundingClientRect();
                    submitBtn.style.width = rect.width + 'px';
                    submitBtn.style.height = rect.height + 'px';
                    
                    // Add loading class to show CSS spinner
                    submitBtn.classList.add('btn-loading');
                } else if (submitBtn.tagName.toLowerCase() === 'input') {
                    submitBtn.disabled = true;
                    submitBtn.value = 'Loading...';
                }
            }
        });

        // ==========================================
        // 4. Dynamic Password Visibility Toggle
        // ==========================================
        function initPasswordToggles() {
            const passwordInputs = document.querySelectorAll('input[type="password"]');
            passwordInputs.forEach(input => {
                if (input.dataset.passwordToggleInitialized === "true") {
                    // If already initialized, just make sure position is recalculated if visible
                    const toggleBtn = input.parentElement?.querySelector(`.dynamic-password-toggle-btn[data-input-id="${input.id}"]`);
                    if (toggleBtn && input.offsetHeight > 0) {
                        const centerOffset = input.offsetTop + (input.offsetHeight / 2);
                        toggleBtn.style.top = centerOffset + 'px';
                    }
                    return;
                }

                const parent = input.parentElement;
                if (!parent) return;

                // Ensure parent position is not static so absolute position works
                const parentStyle = window.getComputedStyle(parent);
                if (parentStyle.position === "static") {
                    parent.style.position = "relative";
                }

                // Skip if there's already a native or custom toggle button
                const existingBtn = parent.querySelector('.show-password, [data-toggle-password]');
                if (existingBtn && !existingBtn.classList.contains('dynamic-password-toggle-btn')) {
                    input.dataset.passwordToggleInitialized = "true";
                    return;
                }

                // Ensure input has an ID
                if (!input.id) {
                    input.id = "pwd_" + Math.random().toString(36).substr(2, 9);
                }

                // Create the toggle button
                const toggleBtn = document.createElement('button');
                toggleBtn.type = 'button';
                toggleBtn.className = 'dynamic-password-toggle-btn';
                toggleBtn.setAttribute('aria-label', 'Show password');
                toggleBtn.setAttribute('data-input-id', input.id);
                toggleBtn.innerHTML = '<i class="fas fa-eye"></i>';

                // Reposition function
                const reposition = () => {
                    if (input.offsetHeight > 0) {
                        const centerOffset = input.offsetTop + (input.offsetHeight / 2);
                        toggleBtn.style.top = centerOffset + 'px';
                    }
                };

                // Adjust padding to avoid text overlapping the eye icon
                const currentPaddingRight = parseInt(window.getComputedStyle(input).paddingRight) || 0;
                if (currentPaddingRight < 40) {
                    input.style.paddingRight = '45px';
                }

                input.after(toggleBtn);

                // Recalculate position immediately and on various focus/hover/input states
                reposition();
                input.addEventListener('focus', reposition);
                input.addEventListener('mouseenter', reposition);
                input.addEventListener('input', reposition);
                window.addEventListener('resize', reposition);

                // Click event listener to toggle password visibility
                toggleBtn.addEventListener('click', function (e) {
                    e.preventDefault();
                    e.stopPropagation();

                    const isPassword = input.type === "password";
                    input.type = isPassword ? "text" : "password";

                    const icon = toggleBtn.querySelector('i');
                    if (icon) {
                        icon.className = isPassword ? 'fas fa-eye-slash' : 'fas fa-eye';
                    }
                    toggleBtn.setAttribute('aria-label', isPassword ? 'Hide password' : 'Show password');
                    
                    // Keep focus on input for better UX
                    input.focus();
                    reposition();
                });

                input.dataset.passwordToggleInitialized = "true";
            });
        }

        // Initialize immediately and after DOM content loads (to catch any late elements)
        initPasswordToggles();
        
        // Also run it on window load and potential dynamic changes
        window.addEventListener('load', initPasswordToggles);
        
        // In case dynamic elements show up (e.g. switching standard/OTP blocks)
        document.addEventListener('click', function() {
            setTimeout(initPasswordToggles, 100);
        });
    });
})();
