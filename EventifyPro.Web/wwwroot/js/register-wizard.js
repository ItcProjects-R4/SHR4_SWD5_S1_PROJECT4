document.addEventListener("DOMContentLoaded", () => {
    const steps = [...document.querySelectorAll(".wizard-step")];
    const prevBtn = document.getElementById("prevStepBtn");
    const nextBtn = document.getElementById("nextStepBtn");
    const submitBtn = document.getElementById("createAccountBtn");
    const form = document.getElementById("registerWizardForm");
    const userNameInput = document.getElementById("userName");
    const userNameStatus = document.getElementById("userNameStatus");
    const checkUserNameBtn = document.getElementById("checkUserNameBtn");
    const passwordInput = document.getElementById("registerPassword");
    const confirmInput = document.getElementById("confirmPassword");
    const passwordError = document.getElementById("password-error");
    const confirmError = document.getElementById("confirm-password-error");
    const passwordRegex = /^(?=.*[A-Za-z])(?=.*\d)(?=.*[\x40$!%*#?&])[A-Za-z\d\x40$!%*#?&]{8,}$/;

    let currentStep = 0;
    let userNameAvailable = false;

    const getActiveSteps = () => {
        const selectedRole = document.querySelector('input[name="Role"]:checked')?.value || "";
        return steps.filter(step => {
            if ((step.id === "orgStep" || step.id === "logoStep") && selectedRole !== "Organizer") {
                return false;
            }
            return true;
        });
    };

    const renderDots = () => {
        const activeSteps = getActiveSteps();
        const progressContainer = document.querySelector(".wizard-progress");
        if (!progressContainer) return;
        
        progressContainer.innerHTML = "";
        activeSteps.forEach((step, i) => {
            const dot = document.createElement("button");
            dot.className = "wizard-dot" + (i === currentStep ? " active" : "") + (i < currentStep ? " completed" : "");
            dot.type = "button";
            dot.textContent = i + 1;
            dot.dataset.stepTarget = i;
            dot.addEventListener("click", () => {
                if (canMoveToStep(i)) {
                    showStep(i);
                }
            });
            progressContainer.appendChild(dot);
        });
    };

    const showStep = (index) => {
        const activeSteps = getActiveSteps();
        currentStep = Math.max(0, Math.min(index, activeSteps.length - 1));

        // Hide all steps, then show only the active one
        steps.forEach(step => step.classList.remove("active"));
        activeSteps[currentStep].classList.add("active");

        // Update step index indicators in page titles dynamically
        activeSteps.forEach((step, i) => {
            const headingSpan = step.querySelector(".step-heading span");
            if (headingSpan) {
                headingSpan.textContent = `Step ${i + 1}`;
            }
        });

        renderDots();

        prevBtn.style.visibility = currentStep === 0 ? "hidden" : "visible";
        nextBtn.style.display = currentStep === activeSteps.length - 1 ? "none" : "inline-flex";
        submitBtn.style.display = currentStep === activeSteps.length - 1 ? "inline-flex" : "none";
    };

    const setStatus = (text, state) => {
        userNameStatus.textContent = text;
        userNameStatus.className = `availability-message ${state}`;
    };

    const validateStep = (stepIndex) => {
        const activeSteps = getActiveSteps();
        const activeStep = activeSteps[stepIndex];

        if (activeStep.id === "orgStep") {
            const orgNameInput = document.getElementById("organizationName");
            if (orgNameInput && !orgNameInput.value.trim()) {
                showStep(stepIndex);
                orgNameInput.setCustomValidity("Organization Name is required.");
                orgNameInput.reportValidity();
                return false;
            } else if (orgNameInput) {
                orgNameInput.setCustomValidity("");
            }
        }

        const fields = [...activeStep.querySelectorAll("input[required]")];
        for (const field of fields) {
            if (!field.checkValidity()) {
                showStep(stepIndex);
                setTimeout(() => field.reportValidity(), 0);
                return false;
            }
        }

        const hasUserName = activeStep.querySelector("#userName");
        if (hasUserName && !userNameAvailable) {
            showStep(stepIndex);
            setStatus("Please check that your username is available first.", "error");
            return false;
        }

        return true;
    };

    const validateCurrentStep = () => validateStep(currentStep);

    const canMoveToStep = (targetStep) => {
        if (targetStep <= currentStep) {
            return true;
        }

        for (let stepIndex = currentStep; stepIndex < targetStep; stepIndex++) {
            if (!validateStep(stepIndex)) {
                return false;
            }
        }

        return true;
    };

    const validatePassword = () => {
        if (!passwordInput.value) {
            passwordError.textContent = "";
            return false;
        }

        if (!passwordRegex.test(passwordInput.value)) {
            passwordError.textContent = "Must be 8+ characters with letters, numbers & special characters.";
            return false;
        }

        passwordError.textContent = "";
        return true;
    };

    const validateConfirmPassword = () => {
        if (!confirmInput.value) {
            confirmError.textContent = "";
            return false;
        }

        if (passwordInput.value !== confirmInput.value) {
            confirmError.textContent = "Passwords do not match.";
            return false;
        }

        confirmError.textContent = "";
        return true;
    };

    document.querySelectorAll("[data-toggle-password]").forEach((button) => {
        button.addEventListener("click", () => {
            const input = document.getElementById(button.dataset.togglePassword);
            const icon = button.querySelector("i");
            if (!input || !icon) return;

            const showPassword = input.type === "password";
            input.type = showPassword ? "text" : "password";
            icon.className = showPassword ? "fas fa-eye-slash" : "fas fa-eye";
            button.setAttribute("aria-label", showPassword ? "Hide password" : "Show password");
        });
    });

    checkUserNameBtn.addEventListener("click", async () => {
        const userName = userNameInput.value.trim();
        userNameAvailable = false;

        if (!userName) {
            setStatus("Username is required.", "error");
            return;
        }

        if (userName.length < 4) {
            setStatus("Username must be at least 4 characters long.", "error");
            return;
        }

        const usernameRegex = /^[a-zA-Z0-9_]+$/;
        if (!usernameRegex.test(userName)) {
            setStatus("Username can contain letters, numbers, and underscores only.", "error");
            return;
        }

        setStatus("Checking username...", "checking");

        try {
            const response = await fetch(`/Account/CheckUserName?userName=${encodeURIComponent(userName)}`);
            const result = await response.json();
            userNameAvailable = result.available === true;
            setStatus(result.message, userNameAvailable ? "success" : "error");
        } catch {
            setStatus("Could not check username right now. Please try again.", "error");
        }
    });

    userNameInput.addEventListener("input", () => {
        userNameAvailable = false;
        setStatus("", "");
    });

    nextBtn.addEventListener("click", () => {
        if (validateCurrentStep()) {
            showStep(currentStep + 1);
        }
    });

    prevBtn.addEventListener("click", () => showStep(currentStep - 1));

    // Set up listener for role changes to update steps list dynamically
    document.querySelectorAll('input[name="Role"]').forEach(radio => {
        radio.addEventListener("change", () => {
            renderDots();
        });
    });

    // Set up image preview and validation for logo upload
    const logoInput = document.getElementById("logoFileInput");
    const logoPreview = document.getElementById("logoPreview");
    const previewPlaceholder = document.getElementById("previewPlaceholder");
    const logoError = document.getElementById("logoFileValidationError");
    const logoPreviewWrapper = document.getElementById("logoPreviewWrapper");

    if (logoInput) {
        logoInput.addEventListener("change", function () {
            const file = this.files[0];
            if (logoError) logoError.textContent = "";

            if (file) {
                // Check file size (2MB limit)
                if (file.size > 2 * 1024 * 1024) {
                    if (logoError) logoError.textContent = "Logo size must not exceed 2MB.";
                    this.value = ""; // Clear file
                    if (logoPreview) logoPreview.style.display = "none";
                    if (previewPlaceholder) previewPlaceholder.style.display = "flex";
                    return;
                }

                // Check file type
                if (!file.type.startsWith("image/")) {
                    if (logoError) logoError.textContent = "Please select a valid image file.";
                    this.value = ""; // Clear file
                    if (logoPreview) logoPreview.style.display = "none";
                    if (previewPlaceholder) previewPlaceholder.style.display = "flex";
                    return;
                }

                const reader = new FileReader();
                reader.addEventListener("load", function () {
                    if (logoPreview) {
                        logoPreview.setAttribute("src", this.result);
                        logoPreview.style.display = "block";
                    }
                    if (previewPlaceholder) previewPlaceholder.style.display = "none";
                });
                reader.readAsDataURL(file);
            } else {
                if (logoPreview) logoPreview.style.display = "none";
                if (previewPlaceholder) previewPlaceholder.style.display = "flex";
            }
        });
    }

    // Set up drag and drop file upload
    if (logoPreviewWrapper && logoInput) {
        ["dragenter", "dragover"].forEach(eventName => {
            logoPreviewWrapper.addEventListener(eventName, (e) => {
                e.preventDefault();
                e.stopPropagation();
                logoPreviewWrapper.classList.add("drag-over");
            }, false);
        });

        ["dragleave", "dragend", "drop"].forEach(eventName => {
            logoPreviewWrapper.addEventListener(eventName, (e) => {
                e.preventDefault();
                e.stopPropagation();
                logoPreviewWrapper.classList.remove("drag-over");
            }, false);
        });

        logoPreviewWrapper.addEventListener("drop", (e) => {
            const dt = e.dataTransfer;
            const files = dt.files;
            if (files && files.length > 0) {
                logoInput.files = files;
                logoInput.dispatchEvent(new Event("change"));
            }
        }, false);
    }

    const passwordStrengthWrapper = document.getElementById("passwordStrengthWrapper");
    const passwordStrengthText = document.getElementById("passwordStrengthText");
    const segments = [...document.querySelectorAll("#passwordStrengthWrapper .strength-segment")];

    const updatePasswordStrength = () => {
        const val = passwordInput.value;
        if (!val) {
            passwordStrengthWrapper.style.display = "none";
            return;
        }

        passwordStrengthWrapper.style.display = "block";
        let score = 0;
        
        if (val.length >= 8) {
            score = 1;
            const hasLetters = /[a-zA-Z]/.test(val);
            const hasNumbers = /\d/.test(val);
            const hasSpecial = /[\x40$!%*#?&]/.test(val);
            
            if (hasLetters && hasNumbers) {
                score = 2;
            }
            if (hasLetters && hasNumbers && hasSpecial) {
                score = 3;
            }
        } else {
            score = 1;
        }

        passwordStrengthText.className = "strength-text";
        segments.forEach(seg => seg.style.backgroundColor = "transparent");

        if (score === 1) {
            passwordStrengthText.textContent = "Weak password";
            passwordStrengthText.classList.add("weak");
            segments[0].style.backgroundColor = "#dc3545";
        } else if (score === 2) {
            passwordStrengthText.textContent = "Medium password";
            passwordStrengthText.classList.add("medium");
            segments[0].style.backgroundColor = "#ffc107";
            segments[1].style.backgroundColor = "#ffc107";
        } else if (score === 3) {
            passwordStrengthText.textContent = "Strong password";
            passwordStrengthText.classList.add("strong");
            segments[0].style.backgroundColor = "#28a745";
            segments[1].style.backgroundColor = "#28a745";
            segments[2].style.backgroundColor = "#28a745";
        }
    };

    passwordInput.addEventListener("input", () => {
        validatePassword();
        updatePasswordStrength();
        if (confirmInput.value) validateConfirmPassword();
    });
    confirmInput.addEventListener("input", validateConfirmPassword);

    const acceptTermsCheckbox = document.getElementById("AcceptTerms");
    const termsWarning = document.getElementById("termsWarning");

    if (termsWarning) termsWarning.style.display = "none";

    if (acceptTermsCheckbox) {
        acceptTermsCheckbox.addEventListener("change", () => {
            if (acceptTermsCheckbox.checked) {
                if (termsWarning) termsWarning.style.display = "none";
                const checkboxGroup = acceptTermsCheckbox.closest(".checkbox-group");
                if (checkboxGroup) {
                    checkboxGroup.classList.remove("has-error");
                    checkboxGroup.style.animation = "none";
                }
            }
        });
    }

    form.addEventListener("submit", (e) => {
        const isPasswordValid = validatePassword();
        const isConfirmValid = validateConfirmPassword();
        let hasErrors = false;

        if (acceptTermsCheckbox && !acceptTermsCheckbox.checked) {
            hasErrors = true;

            if (termsWarning) {
                termsWarning.style.display = "flex";
                termsWarning.style.animation = "none";
                termsWarning.offsetHeight;
                termsWarning.style.animation = null;
            }

            const checkboxGroup = acceptTermsCheckbox.closest(".checkbox-group");
            if (checkboxGroup) {
                checkboxGroup.classList.add("has-error");
                checkboxGroup.style.animation = "none";
                checkboxGroup.offsetHeight;
                checkboxGroup.style.animation = "shakeTerms 0.4s ease-in-out forwards";
            }
        }

        if (!isPasswordValid || !isConfirmValid || hasErrors) {
            e.preventDefault();
            e.stopPropagation();
            const activeSteps = getActiveSteps();
            showStep(activeSteps.length - 1);
        }
    });

    // If there are server-side validation errors, jump to the first step containing an error
    const firstError = form.querySelector(".input-validation-error, .field-validation-error");
    if (firstError) {
        const stepWithError = firstError.closest(".wizard-step");
        if (stepWithError) {
            const activeSteps = getActiveSteps();
            const errorStepIndex = activeSteps.indexOf(stepWithError);
            if (errorStepIndex !== -1) {
                showStep(errorStepIndex);
            } else {
                showStep(0);
            }
        } else {
            showStep(0);
        }
    } else {
        showStep(0);
    }
});
