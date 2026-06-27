document.addEventListener("DOMContentLoaded", function () {
    const selectAllOrganizers = document.getElementById("selectAllOrganizers");
    const orgCheckboxes = document.querySelectorAll(".org-checkbox");
    const bulkOrgsActionsBar = document.getElementById("bulkOrgsActionsBar");
    const bulkOrgsCount = document.getElementById("bulkOrgsCount");
    const checkedCountOrgs = document.getElementById("checkedCountOrgs");
    const selectionCountOrgs = document.getElementById("selectionCountOrgs");

    function updateOrgsSelectionState() {
        const checkedBoxes = document.querySelectorAll(".org-checkbox:checked");
        const count = checkedBoxes.length;

        if (count > 0) {
            bulkOrgsActionsBar.classList.add("active");
            bulkOrgsCount.innerText = count;
            if (checkedCountOrgs) checkedCountOrgs.innerText = count;
            if (selectionCountOrgs) selectionCountOrgs.style.display = "block";
        } else {
            bulkOrgsActionsBar.classList.remove("active");
            if (selectionCountOrgs) selectionCountOrgs.style.display = "none";
        }

        if (selectAllOrganizers) {
            selectAllOrganizers.checked = count === orgCheckboxes.length && orgCheckboxes.length > 0;
        }
    }

    if (selectAllOrganizers) {
        selectAllOrganizers.addEventListener("change", function () {
            orgCheckboxes.forEach(cb => cb.checked = selectAllOrganizers.checked);
            updateOrgsSelectionState();
        });
    }

    orgCheckboxes.forEach(cb => {
        cb.addEventListener("change", updateOrgsSelectionState);
    });

    window.clearOrgsSelection = function () {
        orgCheckboxes.forEach(cb => cb.checked = false);
        if (selectAllOrganizers) selectAllOrganizers.checked = false;
        updateOrgsSelectionState();
    };

    window.submitBulkOrgsAction = function () {
        const checkedBoxes = document.querySelectorAll(".org-checkbox:checked");
        if (checkedBoxes.length === 0) return;

        const confirmationText = `Are you sure you want to approve all ${checkedBoxes.length} selected organizers?`;
        window.confirmAction(confirmationText, function() {
            const form = document.getElementById('bulkApproveOrgsForm');
            const container = document.getElementById('bulkApproveOrgsIdsContainer');
            container.innerHTML = '';

            checkedBoxes.forEach(cb => {
                const input = document.createElement('input');
                input.type = 'hidden';
                input.name = 'userIds';
                input.value = cb.value;
                container.appendChild(input);
            });

            form.submit();
        });
    };
});

window.switchTab = function (tabId) {
    // Remove active class from all buttons
    document.querySelectorAll('.tab-btn').forEach(btn => {
        btn.classList.remove('active');
    });
    // Add active class to current button
    const currentEvent = window.event;
    if (currentEvent && currentEvent.currentTarget) {
        currentEvent.currentTarget.classList.add('active');
    }

    // Hide all tab panels
    document.querySelectorAll('.tab-panel').forEach(panel => {
        panel.style.display = 'none';
    });
    // Show selected tab panel
    const panel = document.getElementById(tabId);
    if (panel) panel.style.display = 'block';
}

window.openRejectOrgModal = function (userId, orgName) {
    document.getElementById('modalOrgUserId').value = userId;
    document.getElementById('modalOrgTitle').innerText = "Are you sure you want to reject \"" + orgName + "\"? Their account will be downgraded to Attendee.";
    document.getElementById('rejectOrgModal').classList.add('active');
}

window.closeRejectOrgModal = function () {
    document.getElementById('rejectOrgModal').classList.remove('active');
}

window.handleOrgOverlayClick = function (event) {
    if (event.target === document.getElementById('rejectOrgModal')) {
        window.closeRejectOrgModal();
    }
}

window.openRejectEventModal = function (eventId, eventTitle) {
    document.getElementById('modalRejectEventId').value = eventId;
    document.getElementById('modalRejectEventTitle').innerText = "Are you sure you want to reject \"" + eventTitle + "\"? This will notify the organizer.";
    document.getElementById('rejectEventModal').classList.add('active');
}

window.closeRejectEventModal = function () {
    document.getElementById('rejectEventModal').classList.remove('active');
}

window.handleEventOverlayClick = function (event) {
    if (event.target === document.getElementById('rejectEventModal')) {
        window.closeRejectEventModal();
    }
}

// Close modal on Escape key
document.addEventListener('keydown', function(event) {
    if (event.key === 'Escape') {
        window.closeRejectOrgModal();
        window.closeRejectEventModal();
    }
});
