/**
 * ACM AMTICS Admin Panel - Members Management Script
 * Handles table rendering, pagination, real-time search,
 * dynamic modal behavior, and AJAX CRUD operations.
 */

(function () {
    'use strict';

    // Page state
    let currentPage = 1;
    const pageSize = 8;
    let currentSearch = '';
    let searchTimeout = null;

    // View context: 'Dashboard' or 'Members'
    const viewContext = window.adminViewContext || 'Dashboard';
    const isDashboard = viewContext === 'Dashboard';

    // DOM Elements
    const membersTableBody = document.getElementById('membersTableBody');
    const paginationInfo = document.getElementById('paginationInfo');
    const paginationControls = document.getElementById('paginationControls');
    const searchInput = document.getElementById('memberSearchInput');
    const openAddMemberBtn = document.getElementById('openAddMemberBtn');

    // Modal elements (only present on Dashboard)
    const modalBackdrop = document.getElementById('addMemberModal');
    const closeModalBtn = document.getElementById('closeModalBtn');
    const cancelModalBtn = document.getElementById('cancelModalBtn');
    const addMemberForm = document.getElementById('addMemberForm');
    const roleSelect = document.getElementById('memberRole');
    const dynamicEventGroup = document.getElementById('dynamicEventGroup');
    const assignEventSelect = document.getElementById('assignEventSelect');
    const submitBtn = document.getElementById('submitAddMemberBtn');

    // Stat counters on Dashboard
    const statMembersCount = document.getElementById('statMembersCount');

    // Init
    document.addEventListener('DOMContentLoaded', function () {
        loadMembers(1);

        // Search input binding with debounce
        if (searchInput) {
            searchInput.addEventListener('input', function (e) {
                clearTimeout(searchTimeout);
                searchTimeout = setTimeout(function () {
                    currentSearch = e.target.value.trim();
                    currentPage = 1;
                    loadMembers(1);
                }, 250);
            });
        }

        // Modal initialization
        if (modalBackdrop) {
            if (openAddMemberBtn) {
                openAddMemberBtn.addEventListener('click', function () {
                    openModal();
                });
            }

            if (closeModalBtn) closeModalBtn.addEventListener('click', closeModal);
            if (cancelModalBtn) cancelModalBtn.addEventListener('click', closeModal);

            modalBackdrop.addEventListener('click', function (e) {
                if (e.target === modalBackdrop) {
                    closeModal();
                }
            });

            // Role change handler for dynamic "Assign Event" field
            if (roleSelect && dynamicEventGroup) {
                roleSelect.addEventListener('change', function () {
                    handleRoleChange(this.value);
                });
            }

            // Form submission handler
            if (addMemberForm) {
                addMemberForm.addEventListener('submit', handleFormSubmit);
            }

            // Pre-load events for dropdown
            loadEvents();
        }
    });

    // =========================================================================
    // API & Table Functions
    // =========================================================================

    async function loadMembers(page = 1) {
        currentPage = page;
        if (!membersTableBody) return;

        // Render skeleton or loading state
        membersTableBody.innerHTML = `
            <tr>
                <td colspan="7" style="text-align:center; padding: 2.5rem; color: #9CA3AF;">
                    <div style="display:flex; align-items:center; justify-content:center; gap:0.5rem;">
                        <svg class="spinner" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                            <circle cx="12" cy="12" r="10" stroke-opacity="0.25"/>
                            <path d="M12 2a10 10 0 0 1 10 10"/>
                        </svg>
                        <span>Loading members...</span>
                    </div>
                </td>
            </tr>
        `;

        try {
            const url = `/api/members?page=${page}&pageSize=${pageSize}&search=${encodeURIComponent(currentSearch)}`;
            const response = await fetch(url, {
                headers: {
                    'Accept': 'application/json',
                    'X-View-Context': viewContext
                }
            });

            if (!response.ok) {
                throw new Error('Failed to fetch members');
            }

            const data = await response.json();
            renderTable(data);
            renderPagination(data);

            // Update stats if on Dashboard
            if (isDashboard && statMembersCount && data.totalCount !== undefined) {
                statMembersCount.textContent = data.totalCount;
            }
        } catch (error) {
            console.error('Error loading members:', error);
            membersTableBody.innerHTML = `
                <tr>
                    <td colspan="7" style="text-align:center; padding: 2rem; color: #DC2626;">
                        Failed to load members. Please try again.
                    </td>
                </tr>
            `;
        }
    }

    function renderTable(data) {
        if (!data || !data.items || data.items.length === 0) {
            membersTableBody.innerHTML = `
                <tr>
                    <td colspan="7" style="text-align:center; padding: 3rem; color: #9CA3AF;">
                        No members found matching your search.
                    </td>
                </tr>
            `;
            return;
        }

        const startIndex = (data.page - 1) * data.pageSize;
        let html = '';

        data.items.forEach((member, idx) => {
            const rowNumber = startIndex + idx + 1;
            const roleClass = getRoleClass(member.role);
            const statusClass = member.status === 'Active' ? 'badge-status-active' : 'badge-status-inactive';
            const formattedDate = formatDate(member.joinDate);
            const initials = member.initials || getInitials(member.name);

            html += `
                <tr data-id="${escapeHtml(member.id)}">
                    <td class="col-num">${rowNumber}</td>
                    <td>
                        <div class="member-name-cell">
                            ${member.avatarUrl 
                                ? `<img src="${escapeHtml(member.avatarUrl)}" alt="${escapeHtml(member.name)}" class="member-avatar" />`
                                : `<div class="member-avatar-initials">${escapeHtml(initials)}</div>`
                            }
                            <span class="member-name-text">${escapeHtml(member.name)}</span>
                        </div>
                    </td>
                    <td class="member-email-text">${escapeHtml(member.email)}</td>
                    <td>
                        <span class="badge-role ${roleClass}">${escapeHtml(member.role)}</span>
                    </td>
                    <td class="member-date-text">${escapeHtml(formattedDate)}</td>
                    <td>
                        <span class="badge-status ${statusClass}">${escapeHtml(member.status)}</span>
                    </td>
                    <td>
                        <div class="row-actions">
                            <button class="action-icon-btn" title="View details" onclick="window.viewMemberDetails('${escapeHtml(member.id)}')">
                                <svg width="18" height="18" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
                                </svg>
                            </button>
                            <button class="action-icon-btn" title="Edit member" onclick="window.editMember('${escapeHtml(member.id)}')">
                                <svg width="18" height="18" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15.232 5.232l3.536 3.536m-2.036-5.036a2.5 2.5 0 113.536 3.536L6.5 21.036H3v-3.572L16.732 3.732z" />
                                </svg>
                            </button>
                            <button class="action-icon-btn action-delete-btn" title="Delete member" onclick="window.deleteMember('${escapeHtml(member.id)}', '${escapeHtml(member.name)}')">
                                <svg width="18" height="18" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                                </svg>
                            </button>
                        </div>
                    </td>
                </tr>
            `;
        });

        membersTableBody.innerHTML = html;
    }

    function renderPagination(data) {
        if (!paginationInfo || !paginationControls) return;

        const total = data.totalCount || 0;
        const totalPages = data.totalPages || 1;
        const page = data.page || 1;

        if (total === 0) {
            paginationInfo.textContent = 'Showing 0 to 0 of 0 members';
            paginationControls.innerHTML = '';
            return;
        }

        const fromItem = (page - 1) * pageSize + 1;
        const toItem = Math.min(page * pageSize, total);
        paginationInfo.textContent = `Showing ${fromItem} to ${toItem} of ${total} members`;

        let buttonsHtml = '';

        // Previous button (<)
        buttonsHtml += `
            <button class="page-btn" ${page <= 1 ? 'disabled' : ''} onclick="window.goToPage(${page - 1})" aria-label="Previous Page">
                &lsaquo;
            </button>
        `;

        // Render page numbers with smart ellipsis matching the mockup (1, 2, 3, 4, 5, ... 16)
        const visiblePages = [];
        if (totalPages <= 7) {
            for (let i = 1; i <= totalPages; i++) visiblePages.push(i);
        } else {
            if (page <= 4) {
                visiblePages.push(1, 2, 3, 4, 5, '...', totalPages);
            } else if (page >= totalPages - 3) {
                visiblePages.push(1, '...', totalPages - 4, totalPages - 3, totalPages - 2, totalPages - 1, totalPages);
            } else {
                visiblePages.push(1, '...', page - 1, page, page + 1, '...', totalPages);
            }
        }

        visiblePages.forEach(p => {
            if (p === '...') {
                buttonsHtml += `<span class="page-dots">&hellip;</span>`;
            } else {
                const isActive = p === page;
                buttonsHtml += `
                    <button class="page-btn ${isActive ? 'active' : ''}" onclick="window.goToPage(${p})" ${isActive ? 'aria-current="page"' : ''}>
                        ${p}
                    </button>
                `;
            }
        });

        // Next button (>)
        buttonsHtml += `
            <button class="page-btn" ${page >= totalPages ? 'disabled' : ''} onclick="window.goToPage(${page + 1})" aria-label="Next Page">
                &rsaquo;
            </button>
        `;

        paginationControls.innerHTML = buttonsHtml;
    }

    // =========================================================================
    // Modal & Add/Edit Member Functions
    // =========================================================================

    function openModal(memberToEdit = null) {
        if (!modalBackdrop) return;

        const modalTitle = document.getElementById('modalTitle');
        const modalSubtitle = document.getElementById('modalSubtitle');
        const editingMemberIdInput = document.getElementById('editingMemberId');

        if (memberToEdit) {
            if (modalTitle) modalTitle.textContent = 'Edit Member Details';
            if (modalSubtitle) modalSubtitle.textContent = 'Update member profile, role, and event assignments.';
            if (submitBtn) submitBtn.textContent = 'Save Changes';
            if (editingMemberIdInput) editingMemberIdInput.value = memberToEdit.id || memberToEdit.Id;

            // Populate fields
            document.getElementById('memberName').value = memberToEdit.name || memberToEdit.Name || '';
            document.getElementById('memberEmail').value = memberToEdit.email || memberToEdit.Email || '';
            document.getElementById('memberPhone').value = memberToEdit.phone || memberToEdit.Phone || '';
            document.getElementById('memberCountryCode').value = memberToEdit.countryCode || memberToEdit.CountryCode || '+91';
            document.getElementById('memberEnrollment').value = memberToEdit.enrollmentNumber || memberToEdit.EnrollmentNumber || '';
            
            const role = memberToEdit.role || memberToEdit.Role || 'Member';
            if (roleSelect) roleSelect.value = role;

            handleRoleChange(role);

            const eventId = memberToEdit.assignedEventId || memberToEdit.AssignedEventId || '';
            if (assignEventSelect && eventId) {
                assignEventSelect.value = eventId;
            }
        } else {
            if (modalTitle) modalTitle.textContent = 'Add Member';
            if (modalSubtitle) modalSubtitle.textContent = 'Add a new member to ACM Amtics.';
            if (submitBtn) submitBtn.textContent = 'Add Member';
            if (editingMemberIdInput) editingMemberIdInput.value = '';

            if (addMemberForm) addMemberForm.reset();
            if (dynamicEventGroup) dynamicEventGroup.classList.remove('visible');
        }

        modalBackdrop.classList.add('open');
        document.body.style.overflow = 'hidden';

        // Focus first field
        const nameField = document.getElementById('memberName');
        if (nameField) nameField.focus();
    }

    function closeModal() {
        if (!modalBackdrop) return;
        modalBackdrop.classList.remove('open');
        document.body.style.overflow = '';
        const editingMemberIdInput = document.getElementById('editingMemberId');
        if (editingMemberIdInput) editingMemberIdInput.value = '';
        if (addMemberForm) {
            addMemberForm.reset();
            clearValidationErrors();
        }
        if (dynamicEventGroup) {
            dynamicEventGroup.classList.remove('visible');
        }
    }

    function handleRoleChange(role) {
        if (!dynamicEventGroup) return;

        // If Role is "Coordinator" (or non-Member), show "Assign Event" dropdown
        const isNonMember = role && role !== 'Member';
        if (isNonMember) {
            dynamicEventGroup.classList.add('visible');
        } else {
            dynamicEventGroup.classList.remove('visible');
            if (assignEventSelect) assignEventSelect.value = '';
        }
    }

    async function loadEvents() {
        if (!assignEventSelect) return;
        try {
            const res = await fetch('/api/events');
            if (res.ok) {
                const data = await res.json();
                const events = data.items || data;
                assignEventSelect.innerHTML = '<option value="">Select an Event</option>';
                events.forEach(ev => {
                    const opt = document.createElement('option');
                    opt.value = ev.id;
                    opt.textContent = ev.name;
                    assignEventSelect.appendChild(opt);
                });
            }
        } catch (e) {
            console.error('Failed to load events', e);
        }
    }

    async function handleFormSubmit(e) {
        e.preventDefault();
        clearValidationErrors();

        const editingId = document.getElementById('editingMemberId')?.value || '';
        const isEditMode = !!editingId;

        const name = document.getElementById('memberName')?.value.trim() || '';
        const email = document.getElementById('memberEmail')?.value.trim() || '';
        const phone = document.getElementById('memberPhone')?.value.trim() || '';
        const countryCode = document.getElementById('memberCountryCode')?.value || '+91';
        const enrollment = document.getElementById('memberEnrollment')?.value.trim() || '';
        const role = document.getElementById('memberRole')?.value || 'Member';
        const eventId = assignEventSelect?.value || '';

        // Client-side validations
        let isValid = true;

        if (!name || name.length < 2) {
            showFieldError('memberName', 'Please enter a valid full name (at least 2 letters).');
            isValid = false;
        }

        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        if (!email || !emailRegex.test(email)) {
            showFieldError('memberEmail', 'Please enter a valid email address.');
            isValid = false;
        }

        const phoneDigits = phone.replace(/\D/g, '');
        if (!phone || phoneDigits.length < 10) {
            showFieldError('memberPhone', 'Please enter a valid 10-digit phone number.');
            isValid = false;
        }

        if (!enrollment || enrollment.length < 3) {
            showFieldError('memberEnrollment', 'Please enter a valid enrollment number.');
            isValid = false;
        }

        if (!isValid) return;

        // Prepare payload
        const payload = {
            name: name,
            email: email,
            phone: phoneDigits,
            countryCode: countryCode,
            enrollmentNumber: enrollment,
            role: role,
            assignedEventId: eventId || null,
            status: 'Active'
        };

        if (submitBtn) {
            submitBtn.disabled = true;
            submitBtn.textContent = isEditMode ? 'Saving...' : 'Adding...';
        }

        try {
            const url = isEditMode ? `/api/members/${editingId}` : '/api/members';
            const method = isEditMode ? 'PUT' : 'POST';

            const res = await fetch(url, {
                method: method,
                headers: {
                    'Content-Type': 'application/json',
                    'Accept': 'application/json',
                    'X-View-Context': 'Dashboard' // Pass context for server validation
                },
                body: JSON.stringify(payload)
            });

            const result = await res.json();

            if (res.ok) {
                closeModal();
                if (window.showToast) {
                    window.showToast(isEditMode ? `Member "${escapeHtml(name)}" updated successfully!` : `Member "${escapeHtml(name)}" added successfully!`, 'success');
                }
                loadMembers(currentPage);
            } else {
                const errMsg = result.message || 'Failed to save member details. Please verify data.';
                if (window.showToast) {
                    window.showToast(errMsg, 'error');
                }
            }
        } catch (err) {
            console.error('Error saving member:', err);
            if (window.showToast) {
                window.showToast('Network error while connecting to server.', 'error');
            }
        } finally {
            if (submitBtn) {
                submitBtn.disabled = false;
                submitBtn.textContent = isEditMode ? 'Save Changes' : 'Add Member';
            }
        }
    }

    function showFieldError(fieldId, message) {
        const errorEl = document.getElementById(fieldId + 'Error');
        if (errorEl) {
            errorEl.textContent = message;
            errorEl.style.display = 'block';
        }
    }

    function clearValidationErrors() {
        const errors = document.querySelectorAll('.form-error-msg');
        errors.forEach(e => {
            e.textContent = '';
            e.style.display = 'none';
        });
    }

    // =========================================================================
    // Helpers & Window Exports
    // =========================================================================

    function getRoleClass(role) {
        switch ((role || '').toLowerCase()) {
            case 'president': return 'badge-role-president';
            case 'vice president': return 'badge-role-vice-president';
            case 'technical head': return 'badge-role-technical-head';
            case 'event head': return 'badge-role-event-head';
            case 'coordinator': return 'badge-role-coordinator';
            default: return 'badge-role-member';
        }
    }

    function formatDate(dateStr) {
        if (!dateStr) return '-';
        const d = new Date(dateStr);
        if (isNaN(d.getTime())) return dateStr;
        const day = d.getDate();
        const monthNames = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];
        const month = monthNames[d.getMonth()];
        const year = d.getFullYear();
        return `${day} ${month} ${year}`;
    }

    function getInitials(name) {
        if (!name) return 'M';
        const parts = name.trim().split(/\s+/);
        if (parts.length >= 2) {
            return (parts[0][0] + parts[1][0]).toUpperCase();
        }
        return name.substring(0, 2).toUpperCase();
    }

    function escapeHtml(text) {
        if (!text) return '';
        return String(text)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#039;');
    }

    // Global window functions for table buttons
    window.goToPage = function (page) {
        loadMembers(page);
    };

    window.deleteMember = async function (id, name) {
        if (!confirm(`Are you sure you want to delete member "${name}"?`)) {
            return;
        }

        try {
            const res = await fetch(`/api/members/${id}`, {
                method: 'DELETE',
                headers: {
                    'Accept': 'application/json',
                    'X-View-Context': viewContext
                }
            });

            if (res.ok) {
                if (window.showToast) {
                    window.showToast(`Member "${name}" was deleted.`, 'success');
                }
                loadMembers(currentPage);
            } else {
                if (window.showToast) {
                    window.showToast('Failed to delete member.', 'error');
                }
            }
        } catch (e) {
            console.error('Error deleting member', e);
            if (window.showToast) {
                window.showToast('Network error while deleting member.', 'error');
            }
        }
    };

    window.viewMemberDetails = async function (id) {
        try {
            const res = await fetch(`/api/members/${id}`);
            if (res.ok) {
                const member = await res.json();
                alert(`Member Details:\n\nName: ${member.name}\nRole: ${member.role}\nEmail: ${member.email}\nPhone: ${member.countryCode} ${member.phone}\nEnrollment: ${member.enrollmentNumber}\nStatus: ${member.status}\nAssigned Event: ${member.assignedEventName || 'None'}`);
            }
        } catch (e) {
            console.error(e);
        }
    };

    window.editMember = async function (id) {
        try {
            const res = await fetch(`/api/members/${id}`);
            if (res.ok) {
                const member = await res.json();
                openModal(member);
            } else {
                if (window.showToast) window.showToast('Failed to load member details for editing.', 'error');
            }
        } catch (e) {
            console.error('Error fetching member details for edit:', e);
        }
    };

})();
