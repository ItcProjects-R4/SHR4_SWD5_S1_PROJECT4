// Scripts for Admin Users Management Page

function openEditModal(userId, fullName, role, isActive) {
    const modalUserId = document.getElementById('modalUserId');
    const modalUserTitle = document.getElementById('modalUserTitle');
    const modalUserRole = document.getElementById('modalUserRole');
    const modalUserActive = document.getElementById('modalUserActive');
    const editUserModal = document.getElementById('editUserModal');

    if (modalUserId) modalUserId.value = userId;
    if (modalUserTitle) modalUserTitle.innerText = fullName;
    if (modalUserRole) modalUserRole.value = role;
    if (modalUserActive) modalUserActive.checked = isActive;
    if (editUserModal) editUserModal.classList.add('active');
}

function closeEditModal() {
    const editUserModal = document.getElementById('editUserModal');
    if (editUserModal) {
        editUserModal.classList.remove('active');
    }
}

function handleOverlayClick(event) {
    const editUserModal = document.getElementById('editUserModal');
    if (event.target === editUserModal) {
        closeEditModal();
    }
}

// Close modal on Escape key
document.addEventListener('keydown', function(event) {
    if (event.key === 'Escape') {
        closeEditModal();
    }
});
