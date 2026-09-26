/* ==========================================================================
   Events & Attendance Management JavaScript
   Page 1 (/Events) & Page 2 (Add Event Modal)
   ========================================================================== */

(function () {
    'use strict';

    let currentPage = 1;
    const pageSize = 8;
    let currentSearch = '';
    let searchTimeout = null;

    // DOM Elements
    const eventsTableBody = document.getElementById('eventsTableBody');
    const eventPaginationInfo = document.getElementById('eventPaginationInfo');
    const eventPaginationControls = document.getElementById('eventPaginationControls');
    const eventSearchInput = document.getElementById('eventSearchInput');

    const statTotalEvents = document.getElementById('statTotalEvents');
    const statEventsChange = document.getElementById('statEventsChange');
    const statTotalAttendees = document.getElementById('statTotalAttendees');
    const statAttendeesChange = document.getElementById('statAttendeesChange');

    // Add Event Modal Elements
    const addEventModalOverlay = document.getElementById('addEventModalOverlay');
    const openAddEventModalBtn = document.getElementById('openAddEventModalBtn');
    const closeAddEventModalBtn = document.getElementById('closeAddEventModalBtn');
    const cancelAddEventBtn = document.getElementById('cancelAddEventBtn');
    const addEventForm = document.getElementById('addEventForm');
    const submitAddEventBtn = document.getElementById('submitAddEventBtn');



    // Initialize
    document.addEventListener('DOMContentLoaded', () => {
        loadEventsStats();
        loadEvents();
        setupSearch();
        setupModal();
    });

    // --------------------------------------------------------------------------
    // 1. Fetch & Display Stats
    // --------------------------------------------------------------------------
    async function loadEventsStats() {
        try {
            const res = await fetch('/api/events/stats');
            if (res.ok) {
                const data = await res.json();
                if (statTotalEvents) statTotalEvents.textContent = data.totalEvents;
                if (statEventsChange) statEventsChange.textContent = data.eventsChangePercentage;
                if (statTotalAttendees) statTotalAttendees.textContent = data.totalAttendees;
                if (statAttendeesChange) statAttendeesChange.textContent = data.attendeesChangePercentage;
            }
        } catch (err) {
            console.error('Error fetching event statistics:', err);
        }
    }

    // --------------------------------------------------------------------------
    // 2. Fetch & Render Events Table
    // --------------------------------------------------------------------------
    async function loadEvents() {
        if (!eventsTableBody) return;

        eventsTableBody.innerHTML = `
            <tr>
                <td colspan="6" style="text-align: center; padding: 3rem 1rem; color: var(--text-muted);">
                    <div style="display: flex; align-items: center; justify-content: center; gap: 0.5rem;">
                        <svg class="animate-spin" width="20" height="20" fill="none" viewBox="0 0 24 24">
                            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8H4z"></path>
                        </svg>
                        <span>Loading events...</span>
                    </div>
                </td>
            </tr>
        `;

        try {
            const url = `/api/events?search=${encodeURIComponent(currentSearch)}&page=${currentPage}&pageSize=${pageSize}`;
            const res = await fetch(url);
            if (!res.ok) throw new Error('Failed to load events.');

            const data = await res.json();
            renderEvents(data.items, data.page, data.pageSize, data.totalCount);
        } catch (err) {
            console.error('Error loading events:', err);
            eventsTableBody.innerHTML = `
                <tr>
                    <td colspan="6" style="text-align: center; padding: 2.5rem 1rem; color: var(--danger-text);">
                        Failed to load events. Please refresh the page.
                    </td>
                </tr>
            `;
        }
    }

    function renderEvents(items, page, pageSize, totalCount) {
        if (!items || items.length === 0) {
            eventsTableBody.innerHTML = `
                <tr>
                    <td colspan="6" style="text-align: center; padding: 3rem 1rem; color: var(--text-secondary);">
                        <p style="font-weight: 600; font-size: 0.95rem;">No events found</p>
                        <p style="font-size: 0.8125rem; color: var(--text-muted); margin-top: 0.35rem;">
                            Try adjusting your search criteria or add a new event.
                        </p>
                    </td>
                </tr>
            `;
            if (eventPaginationInfo) eventPaginationInfo.textContent = 'Showing 0 to 0 of 0 events';
            if (eventPaginationControls) eventPaginationControls.innerHTML = '';
            return;
        }

        const startIdx = (page - 1) * pageSize + 1;
        const endIdx = Math.min(page * pageSize, totalCount);

        let html = '';
        items.forEach((ev, idx) => {
            const rowNumber = startIdx + idx;
            const eventDate = new Date(ev.date);
            const dateFormatted = eventDate.toLocaleDateString('en-GB', {
                day: 'numeric',
                month: 'short',
                year: 'numeric'
            });

            // Color-coded Category Badge
            const cat = (ev.category || 'Workshop').toLowerCase();
            let catBadgeClass = 'badge-type-workshop';
            if (cat.includes('seminar')) catBadgeClass = 'badge-type-seminar';
            else if (cat.includes('competition') || cat.includes('hackathon')) catBadgeClass = 'badge-type-competition';
            else if (cat.includes('talk')) catBadgeClass = 'badge-type-talk';

            // Today badge
            const isToday = ev.isToday || (rowNumber === 1 && currentPage === 1);
            const todayBadgeHtml = isToday ? '<span class="badge-today">Today</span>' : '';



            html += `
                <tr>
                    <td class="col-num">${rowNumber}</td>
                    <td>
                        <div style="display: flex; align-items: center;">
                            <span style="font-weight: 600; color: var(--text-primary);">${escapeHtml(ev.name)}</span>
                            ${todayBadgeHtml}
                        </div>
                    </td>
                    <td>
                        <div style="display: inline-flex; align-items: center; gap: 0.45rem; color: var(--text-secondary); font-size: 0.8125rem;">
                            <svg width="15" height="15" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
                            </svg>
                            <span>${dateFormatted}</span>
                        </div>
                    </td>
                    <td>
                        <span class="badge-type ${catBadgeClass}">
                            ${escapeHtml(ev.category || 'Workshop')}
                        </span>
                    </td>
                    <td>
                        <div class="attendees-cell-wrapper">
                            <span class="attendees-count-text">
                                <svg width="16" height="16" fill="none" viewBox="0 0 24 24" stroke="currentColor" style="color: var(--text-muted);">
                                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 20h5v-2a3 3 0 00-5-3.512M9 20H4v-2a3 3 0 015-3.512M12 11a4 4 0 100-8 4 4 0 000 8zm0 2a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
                                </svg>
                                <span>${ev.attendeesCount ?? 0}</span>
                            </span>
                        </div>
                    </td>
                    <td>
                        <div class="actions-cell-events">
                            <!-- View Icon: Navigates to /Events/{id}/Attendees -->
                            <a href="/Events/${ev.id}/Attendees" class="btn-action-icon" title="View Attendees">
                                <svg width="17" height="17" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
                                </svg>
                            </a>
                            <!-- Edit Icon -->
                            <button type="button" class="btn-action-icon edit-event-btn" data-id="${ev.id}" title="Edit Event">
                                <svg width="16" height="16" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15.232 5.232l3.536 3.536m-2.036-5.036a2.5 2.5 0 113.536 3.536L6.5 21.036H3v-3.572L16.732 3.732z" />
                                </svg>
                            </button>
                            <!-- Delete Icon -->
                            <button type="button" class="btn-action-icon danger delete-event-btn" data-id="${ev.id}" data-name="${escapeHtml(ev.name)}" title="Delete Event">
                                <svg width="16" height="16" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                                </svg>
                            </button>
                        </div>
                    </td>
                </tr>
            `;
        });

        eventsTableBody.innerHTML = html;

        // Update pagination display
        if (eventPaginationInfo) {
            eventPaginationInfo.textContent = `Showing ${startIdx} to ${endIdx} of ${totalCount} events`;
        }

        renderPagination(totalCount, page, pageSize);
        attachRowEventListeners();
    }

    // --------------------------------------------------------------------------
    // 3. Pagination Controls
    // --------------------------------------------------------------------------
    function renderPagination(totalCount, page, pageSize) {
        if (!eventPaginationControls) return;

        const totalPages = Math.ceil(totalCount / pageSize) || 1;
        if (totalPages <= 1) {
            eventPaginationControls.innerHTML = '';
            return;
        }

        let html = '';

        // Previous button (<)
        html += `
            <button class="page-btn page-arrow ${page <= 1 ? 'disabled' : ''}" 
                    data-page="${page - 1}" 
                    aria-label="Previous Page" 
                    ${page <= 1 ? 'disabled' : ''}>
                &lt;
            </button>
        `;

        // Page buttons matching UI design (< 1 2 3 4 ... 16 >)
        const pages = [];
        if (totalPages <= 5) {
            for (let i = 1; i <= totalPages; i++) pages.push(i);
        } else {
            pages.push(1, 2, 3, 4);
            if (totalPages > 5) {
                pages.push('...');
                pages.push(totalPages);
            }
        }

        pages.forEach(p => {
            if (p === '...') {
                html += `<span class="page-ellipsis">...</span>`;
            } else {
                const isActive = p === page ? 'active' : '';
                html += `<button class="page-btn ${isActive}" data-page="${p}">${p}</button>`;
            }
        });

        // Next button (>)
        html += `
            <button class="page-btn page-arrow ${page >= totalPages ? 'disabled' : ''}" 
                    data-page="${page + 1}" 
                    aria-label="Next Page" 
                    ${page >= totalPages ? 'disabled' : ''}>
                &gt;
            </button>
        `;

        eventPaginationControls.innerHTML = html;

        eventPaginationControls.querySelectorAll('.page-btn:not(.disabled)').forEach(btn => {
            btn.addEventListener('click', () => {
                const targetPage = parseInt(btn.getAttribute('data-page'), 10);
                if (!isNaN(targetPage) && targetPage !== currentPage) {
                    currentPage = targetPage;
                    loadEvents();
                    window.scrollTo({ top: 0, behavior: 'smooth' });
                }
            });
        });
    }

    // --------------------------------------------------------------------------
    // 4. Search Handler
    // --------------------------------------------------------------------------
    function setupSearch() {
        if (!eventSearchInput) return;

        eventSearchInput.addEventListener('input', (e) => {
            clearTimeout(searchTimeout);
            searchTimeout = setTimeout(() => {
                currentSearch = e.target.value.trim();
                currentPage = 1;
                loadEvents();
            }, 300);
        });
    }

    // --------------------------------------------------------------------------
    // 5. Add Event Modal Handling
    // --------------------------------------------------------------------------
    // 5. Add/Edit Event Modal Handling
    // --------------------------------------------------------------------------
    const markAsCompletedBtn = document.getElementById('markAsCompletedBtn');
    const eventStatusInput = document.getElementById('eventStatusInput');
    const statusText = document.getElementById('statusText');
    const markCompletedBtnText = document.getElementById('markCompletedBtnText');
    const markCompletedCheckIcon = document.getElementById('markCompletedCheckIcon');

    function updateStatusUI(status) {
        if (!eventStatusInput) return;
        const isCompleted = (status || '').toLowerCase() === 'completed';
        eventStatusInput.value = isCompleted ? 'Completed' : 'Upcoming';
        if (statusText) statusText.textContent = isCompleted ? 'Completed' : 'Upcoming';
        if (markCompletedBtnText) markCompletedBtnText.textContent = isCompleted ? 'Completed ✓' : 'Mark as Completed';
        if (markCompletedCheckIcon) markCompletedCheckIcon.style.display = isCompleted ? 'inline-block' : 'none';
        if (markAsCompletedBtn) {
            if (isCompleted) {
                markAsCompletedBtn.style.background = 'rgba(16, 185, 129, 0.15)';
                markAsCompletedBtn.style.color = '#34d399';
                markAsCompletedBtn.style.borderColor = 'rgba(16, 185, 129, 0.3)';
            } else {
                markAsCompletedBtn.style.background = '';
                markAsCompletedBtn.style.color = '';
                markAsCompletedBtn.style.borderColor = '';
            }
        }
    }

    function setupModal() {
        if (openAddEventModalBtn) {
            openAddEventModalBtn.addEventListener('click', () => {
                openAddModal();
            });
        }

        if (markAsCompletedBtn) {
            markAsCompletedBtn.addEventListener('click', () => {
                const current = (eventStatusInput.value || '').toLowerCase();
                const next = current === 'completed' ? 'Upcoming' : 'Completed';
                updateStatusUI(next);
            });
        }

        if (closeAddEventModalBtn) closeAddEventModalBtn.addEventListener('click', closeAddModal);
        if (cancelAddEventBtn) cancelAddEventBtn.addEventListener('click', closeAddModal);

        if (addEventModalOverlay) {
            addEventModalOverlay.addEventListener('click', (e) => {
                if (e.target === addEventModalOverlay) closeAddModal();
            });
        }

        if (addEventForm) {
            addEventForm.addEventListener('submit', async (e) => {
                e.preventDefault();
                await handleAddEventSubmit();
            });
        }
    }

    function openModalContainer() {
        if (!addEventModalOverlay) return;
        addEventModalOverlay.classList.add('open');
        addEventModalOverlay.setAttribute('aria-hidden', 'false');
        document.body.style.overflow = 'hidden';

        setTimeout(() => {
            const firstInput = document.getElementById('eventNameInput');
            if (firstInput) firstInput.focus();
        }, 100);
    }

    function openAddModal() {
        if (!addEventModalOverlay) return;

        if (addEventForm) addEventForm.reset();
        clearFormErrors();

        const eventIdInput = document.getElementById('eventIdInput');
        const modalTitle = document.getElementById('addEventModalTitle');
        const modalSubtitle = document.getElementById('addEventModalSubtitle');
        const descInput = document.getElementById('eventDescriptionInput');
        const imgInput = document.getElementById('eventImageUrlInput');

        if (eventIdInput) eventIdInput.value = '';
        if (modalTitle) modalTitle.textContent = 'Add Event';
        if (modalSubtitle) modalSubtitle.textContent = 'Create a new event for ACM Amtics.';
        if (submitAddEventBtn) submitAddEventBtn.textContent = 'Add Event';
        if (descInput) descInput.value = '';
        if (imgInput) imgInput.value = '';
        
        const dateInput = document.getElementById('eventDateInput');
        if (dateInput) {
            dateInput.value = new Date().toISOString().split('T')[0];
        }

        updateStatusUI('Upcoming');
        openModalContainer();
    }

    async function openEditModal(id) {
        if (!addEventModalOverlay) return;
        try {
            clearFormErrors();
            const res = await fetch(`/api/events/${id}`);
            if (!res.ok) throw new Error('Failed to fetch event details.');
            const ev = await res.json();

            const eventIdInput = document.getElementById('eventIdInput');
            const modalTitle = document.getElementById('addEventModalTitle');
            const modalSubtitle = document.getElementById('addEventModalSubtitle');
            const nameInput = document.getElementById('eventNameInput');
            const dateInput = document.getElementById('eventDateInput');
            const timeInput = document.getElementById('eventTimeInput');
            const venueInput = document.getElementById('eventVenueInput');
            const categoryInput = document.getElementById('eventCategoryInput');
            const descInput = document.getElementById('eventDescriptionInput');
            const imgInput = document.getElementById('eventImageUrlInput');

            if (eventIdInput) eventIdInput.value = ev.id || '';
            if (modalTitle) modalTitle.textContent = 'Edit Event';
            if (modalSubtitle) modalSubtitle.textContent = 'Update details for this ACM Amtics event.';
            if (submitAddEventBtn) submitAddEventBtn.textContent = 'Save Changes';

            if (nameInput) nameInput.value = ev.name || '';
            if (dateInput && ev.date) {
                dateInput.value = new Date(ev.date).toISOString().split('T')[0];
            }
            if (timeInput) timeInput.value = ev.time || '10:00 AM - 1:00 PM';
            if (venueInput) venueInput.value = ev.venue || '';
            if (categoryInput) categoryInput.value = ev.category || 'Workshop';
            if (descInput) descInput.value = ev.description || '';
            if (imgInput) imgInput.value = ev.imageUrl || '';

            updateStatusUI(ev.status || 'Upcoming');
            openModalContainer();
        } catch (err) {
            console.error('Error opening edit modal:', err);
            showToast('Error loading event data.', 'error');
        }
    }

    function closeAddModal() {
        if (!addEventModalOverlay) return;
        addEventModalOverlay.classList.remove('open');
        addEventModalOverlay.setAttribute('aria-hidden', 'true');
        document.body.style.overflow = '';
        if (addEventForm) addEventForm.reset();
        clearFormErrors();
    }

    function clearFormErrors() {
        document.querySelectorAll('.form-error-msg').forEach(el => el.style.display = 'none');
    }

    async function handleAddEventSubmit() {
        clearFormErrors();

        const eventIdInput = document.getElementById('eventIdInput');
        const isEdit = eventIdInput && eventIdInput.value.trim().length > 0;
        const eventId = isEdit ? eventIdInput.value.trim() : null;

        const nameInput = document.getElementById('eventNameInput');
        const dateInput = document.getElementById('eventDateInput');
        const timeInput = document.getElementById('eventTimeInput');
        const venueInput = document.getElementById('eventVenueInput');
        const categoryInput = document.getElementById('eventCategoryInput');
        const descInput = document.getElementById('eventDescriptionInput');
        const imgInput = document.getElementById('eventImageUrlInput');

        let hasError = false;

        if (!nameInput.value.trim()) {
            document.getElementById('eventNameError').style.display = 'block';
            hasError = true;
        }
        if (!dateInput.value) {
            document.getElementById('eventDateError').style.display = 'block';
            hasError = true;
        }
        if (!timeInput.value.trim()) {
            document.getElementById('eventTimeError').style.display = 'block';
            hasError = true;
        }
        if (!venueInput.value.trim()) {
            document.getElementById('eventVenueError').style.display = 'block';
            hasError = true;
        }
        if (!categoryInput.value) {
            document.getElementById('eventCategoryError').style.display = 'block';
            hasError = true;
        }

        if (hasError) return;

        const payload = {
            name: nameInput.value.trim(),
            date: new Date(dateInput.value).toISOString(),
            time: timeInput.value.trim(),
            venue: venueInput.value.trim(),
            category: categoryInput.value.trim(),
            description: descInput ? descInput.value.trim() : `${nameInput.value.trim()} conducted by ACM AMTICS Student Chapter.`,
            imageUrl: imgInput ? imgInput.value.trim() : null,
            status: eventStatusInput ? eventStatusInput.value.trim() : 'Upcoming'
        };

        const url = isEdit ? `/api/events/${eventId}` : '/api/events';
        const method = isEdit ? 'PUT' : 'POST';

        try {
            submitAddEventBtn.disabled = true;
            submitAddEventBtn.textContent = isEdit ? 'Saving...' : 'Adding...';

            const res = await fetch(url, {
                method: method,
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(payload)
            });

            if (!res.ok) {
                const errData = await res.json().catch(() => ({}));
                throw new Error(errData.message || `Failed to ${isEdit ? 'update' : 'create'} event.`);
            }

            closeAddModal();
            showToast(`Event ${isEdit ? 'updated' : 'created'} successfully!`, 'success');
            if (!isEdit) currentPage = 1;
            loadEvents();
            loadEventsStats();
        } catch (err) {
            console.error('Error submitting event:', err);
            showToast(err.message || `Error ${isEdit ? 'updating' : 'creating'} event.`, 'error');
        } finally {
            submitAddEventBtn.disabled = false;
            submitAddEventBtn.textContent = isEdit ? 'Save Changes' : 'Add Event';
        }
    }

    // --------------------------------------------------------------------------
    // 6. Row Event Listeners (Delete, Edit)
    // --------------------------------------------------------------------------
    function attachRowEventListeners() {

        // Delete buttons
        document.querySelectorAll('.delete-event-btn').forEach(btn => {
            btn.addEventListener('click', async () => {
                const id = btn.getAttribute('data-id');
                const name = btn.getAttribute('data-name');
                if (confirm(`Are you sure you want to delete the event "${name}"?`)) {
                    await handleDeleteEvent(id);
                }
            });
        });

        // Edit buttons: Open modal with event prefilled
        document.querySelectorAll('.edit-event-btn').forEach(btn => {
            btn.addEventListener('click', async () => {
                const id = btn.getAttribute('data-id');
                await openEditModal(id);
            });
        });
    }

    async function handleDeleteEvent(id) {
        try {
            const res = await fetch(`/api/events/${id}`, {
                method: 'DELETE'
            });

            if (res.ok) {
                showToast('Event removed successfully.', 'success');
                loadEvents();
                loadEventsStats();
            } else {
                showToast('Failed to delete event.', 'error');
            }
        } catch (err) {
            console.error('Error deleting event:', err);
            showToast('An error occurred while deleting the event.', 'error');
        }
    }

    // --------------------------------------------------------------------------
    // 8. Utilities (Toast & XSS escape)
    // --------------------------------------------------------------------------
    function showToast(message, type = 'success') {
        const container = document.getElementById('toastContainer');
        if (!container) return;

        const isSuccess = type === 'success';
        const iconSvg = isSuccess
            ? `<svg class="toast-icon" width="18" height="18" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/></svg>`
            : `<svg class="toast-icon" width="18" height="18" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/></svg>`;

        container.innerHTML = `
            <div class="toast-card toast-${type}">
                ${iconSvg}
                <span>${escapeHtml(message)}</span>
            </div>
        `;

        container.classList.add('visible');
        setTimeout(() => {
            container.classList.remove('visible');
        }, 3200);
    }

    function escapeHtml(str) {
        if (!str) return '';
        return String(str)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#039;');
    }
})();
