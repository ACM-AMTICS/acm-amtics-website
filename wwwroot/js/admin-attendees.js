/* ==========================================================================
   Attendees Detail Page JavaScript
   Page 3 (/Events/{eventId}/Attendees)
   ========================================================================== */

(function () {
    'use strict';

    const eventId = window.currentEventId;
    if (!eventId) {
        console.error('No Event ID provided.');
        return;
    }

    let currentPage = 1;
    const pageSize = 8;
    let currentSearch = '';
    let searchTimeout = null;

    // DOM Elements
    const attendeesTableBody = document.getElementById('attendeesTableBody');
    const attendeePaginationInfo = document.getElementById('attendeePaginationInfo');
    const attendeePaginationControls = document.getElementById('attendeePaginationControls');
    const attendeeSearchInput = document.getElementById('attendeeSearchInput');
    const attendeesSectionTitle = document.getElementById('attendeesSectionTitle');
    const exportAttendeesBtn = document.getElementById('exportAttendeesBtn');

    // 4 Stat Cards
    const statAttendeesTotal = document.getElementById('statAttendeesTotal');
    const statAttendeesMembers = document.getElementById('statAttendeesMembers');
    const statAttendeesMembersPct = document.getElementById('statAttendeesMembersPct');
    const statAttendeesNonMembers = document.getElementById('statAttendeesNonMembers');
    const statAttendeesNonMembersPct = document.getElementById('statAttendeesNonMembersPct');
    const statAttendanceRate = document.getElementById('statAttendanceRate');
    const statAttendanceRateChange = document.getElementById('statAttendanceRateChange');

    // View Details Modal
    const openEventDetailsBtn = document.getElementById('openEventDetailsBtn');
    const closeEventDetailsModalBtn = document.getElementById('closeEventDetailsModalBtn');
    const dismissEventDetailsBtn = document.getElementById('dismissEventDetailsBtn');
    const eventDetailsModalOverlay = document.getElementById('eventDetailsModalOverlay');

    document.addEventListener('DOMContentLoaded', () => {
        loadAttendeesData();
        setupSearch();
        setupExport();
        setupDetailsModal();
    });

    // --------------------------------------------------------------------------
    // 1. Fetch & Render Attendees and Stats
    // --------------------------------------------------------------------------
    async function loadAttendeesData() {
        if (!attendeesTableBody) return;

        attendeesTableBody.innerHTML = `
            <tr>
                <td colspan="8" style="text-align: center; padding: 3rem 1rem; color: var(--text-muted);">
                    <div style="display: flex; align-items: center; justify-content: center; gap: 0.5rem;">
                        <svg class="animate-spin" width="20" height="20" fill="none" viewBox="0 0 24 24">
                            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8H4z"></path>
                        </svg>
                        <span>Loading attendees...</span>
                    </div>
                </td>
            </tr>
        `;

        try {
            const url = `/api/events/${encodeURIComponent(eventId)}/attendees?search=${encodeURIComponent(currentSearch)}&page=${currentPage}&pageSize=${pageSize}`;
            const res = await fetch(url);
            if (!res.ok) throw new Error('Failed to load attendees.');

            const data = await res.json();

            // Update stats
            if (data.stats) {
                updateStats(data.stats);
            }

            // Update section title
            const total = data.attendees ? data.attendees.totalCount : (data.stats ? data.stats.totalAttendees : 0);
            if (attendeesSectionTitle) {
                attendeesSectionTitle.textContent = `Attendees (${total})`;
            }

            // Render table
            if (data.attendees) {
                renderAttendees(data.attendees.items, data.attendees.page, data.attendees.pageSize, data.attendees.totalCount);
            }
        } catch (err) {
            console.error('Error fetching attendees data:', err);
            attendeesTableBody.innerHTML = `
                <tr>
                    <td colspan="8" style="text-align: center; padding: 2.5rem 1rem; color: var(--danger-text);">
                        Failed to load attendees. Please try again.
                    </td>
                </tr>
            `;
        }
    }

    function updateStats(stats) {
        if (statAttendeesTotal) statAttendeesTotal.textContent = stats.totalAttendees;
        if (statAttendeesMembers) statAttendeesMembers.textContent = stats.membersCount;
        if (statAttendeesMembersPct) statAttendeesMembersPct.textContent = stats.membersPercentage;
        if (statAttendeesNonMembers) statAttendeesNonMembers.textContent = stats.nonMembersCount;
        if (statAttendeesNonMembersPct) statAttendeesNonMembersPct.textContent = stats.nonMembersPercentage;
        if (statAttendanceRate) statAttendanceRate.textContent = stats.attendanceRate;
        if (statAttendanceRateChange) statAttendanceRateChange.textContent = stats.attendanceRateChange;
    }

    function renderAttendees(items, page, pageSize, totalCount) {
        if (!items || items.length === 0) {
            attendeesTableBody.innerHTML = `
                <tr>
                    <td colspan="8" style="text-align: center; padding: 3rem 1rem; color: var(--text-secondary);">
                        <p style="font-weight: 600; font-size: 0.95rem;">No attendees found</p>
                        <p style="font-size: 0.8125rem; color: var(--text-muted); margin-top: 0.35rem;">
                            No matching students found for the current search filter.
                        </p>
                    </td>
                </tr>
            `;
            if (attendeePaginationInfo) attendeePaginationInfo.textContent = 'Showing 0 to 0 of 0 attendees';
            if (attendeePaginationControls) attendeePaginationControls.innerHTML = '';
            return;
        }

        const startIdx = (page - 1) * pageSize + 1;
        const endIdx = Math.min(page * pageSize, totalCount);

        let html = '';
        items.forEach((a, idx) => {
            const rowNumber = startIdx + idx;

            // Avatar or Initials Circle
            let avatarHtml = '';
            if (a.avatarUrl) {
                avatarHtml = `<img src="${escapeHtml(a.avatarUrl)}" alt="${escapeHtml(a.attendeeName)}" class="attendee-avatar-img" />`;
            } else {
                avatarHtml = `<div class="attendee-avatar-initials">${escapeHtml(a.initials || 'ST')}</div>`;
            }

            // Handle
            const handleHtml = a.handle
                ? `<span class="attendee-handle">${escapeHtml(a.handle)}</span>`
                : '';

            // Enrollment No
            const enrollmentDisplay = a.enrollmentNumber ? escapeHtml(a.enrollmentNumber) : '—';

            // Member vs Undefined Badge
            const isMember = (a.type || '').toLowerCase() === 'member';
            const typeBadgeClass = isMember ? 'badge-type-member' : 'badge-type-undefined';
            const typeText = isMember ? 'Member' : 'Undefined';

            // Status Badge
            const isPresent = (a.status || '').toLowerCase() === 'present';
            const statusBadgeClass = isPresent ? 'badge-status-active' : 'badge-status-inactive';
            const statusText = isPresent ? 'Present' : 'Absent';

            // Joined At
            const joinedAtDisplay = a.joinedAtFormatted || '10:00 AM';

            html += `
                <tr>
                    <td class="col-num">${rowNumber}</td>
                    <td>
                        <div class="attendee-user-cell">
                            ${avatarHtml}
                            <div class="attendee-info-col">
                                <span class="attendee-name">${escapeHtml(a.attendeeName)}</span>
                                ${handleHtml}
                            </div>
                        </div>
                    </td>
                    <td>
                        <span style="font-size: 0.8125rem; color: var(--text-secondary);">${escapeHtml(a.email)}</span>
                    </td>
                    <td>
                        <span style="font-size: 0.8125rem; font-family: monospace; color: var(--text-primary); font-weight: 500;">
                            ${enrollmentDisplay}
                        </span>
                    </td>
                    <td>
                        <span class="badge-role ${typeBadgeClass}">
                            ${typeText}
                        </span>
                    </td>
                    <td>
                        <span class="badge-status ${statusBadgeClass}">
                            ${statusText}
                        </span>
                    </td>
                    <td>
                        <span style="font-size: 0.8125rem; color: var(--text-secondary);">
                            ${joinedAtDisplay}
                        </span>
                    </td>
                    <td>
                        <button type="button" class="btn-kebab" title="Actions" aria-label="More actions">
                            &#8942;
                        </button>
                    </td>
                </tr>
            `;
        });

        attendeesTableBody.innerHTML = html;

        if (attendeePaginationInfo) {
            attendeePaginationInfo.textContent = `Showing ${startIdx} to ${endIdx} of ${totalCount} attendees`;
        }

        renderPagination(totalCount, page, pageSize);
    }

    // --------------------------------------------------------------------------
    // 2. Pagination Controls
    // --------------------------------------------------------------------------
    function renderPagination(totalCount, page, pageSize) {
        if (!attendeePaginationControls) return;

        const totalPages = Math.ceil(totalCount / pageSize) || 1;
        if (totalPages <= 1) {
            attendeePaginationControls.innerHTML = '';
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

        // Page buttons matching Screenshot 3 (< 1 2 3 4 5 ... 15 >)
        const pages = [];
        if (totalPages <= 6) {
            for (let i = 1; i <= totalPages; i++) pages.push(i);
        } else {
            pages.push(1, 2, 3, 4, 5);
            pages.push('...');
            pages.push(totalPages);
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

        attendeePaginationControls.innerHTML = html;

        attendeePaginationControls.querySelectorAll('.page-btn:not(.disabled)').forEach(btn => {
            btn.addEventListener('click', () => {
                const targetPage = parseInt(btn.getAttribute('data-page'), 10);
                if (!isNaN(targetPage) && targetPage !== currentPage) {
                    currentPage = targetPage;
                    loadAttendeesData();
                    window.scrollTo({ top: 250, behavior: 'smooth' });
                }
            });
        });
    }

    // --------------------------------------------------------------------------
    // 3. Search Handler
    // --------------------------------------------------------------------------
    function setupSearch() {
        if (!attendeeSearchInput) return;

        attendeeSearchInput.addEventListener('input', (e) => {
            clearTimeout(searchTimeout);
            searchTimeout = setTimeout(() => {
                currentSearch = e.target.value.trim();
                currentPage = 1;
                loadAttendeesData();
            }, 300);
        });
    }

    // --------------------------------------------------------------------------
    // 4. Export CSV (filtered by search query)
    // --------------------------------------------------------------------------
    function setupExport() {
        if (!exportAttendeesBtn) return;

        exportAttendeesBtn.addEventListener('click', () => {
            const exportUrl = `/api/events/${encodeURIComponent(eventId)}/attendees/export?search=${encodeURIComponent(currentSearch)}`;
            showToast('Preparing attendees CSV export...', 'info');
            window.location.href = exportUrl;
        });
    }

    // --------------------------------------------------------------------------
    // 5. Details Modal
    // --------------------------------------------------------------------------
    function setupDetailsModal() {
        if (openEventDetailsBtn) {
            openEventDetailsBtn.addEventListener('click', () => {
                if (eventDetailsModalOverlay) {
                    eventDetailsModalOverlay.classList.add('active');
                    eventDetailsModalOverlay.setAttribute('aria-hidden', 'false');
                    document.body.style.overflow = 'hidden';
                }
            });
        }

        const closeModal = () => {
            if (eventDetailsModalOverlay) {
                eventDetailsModalOverlay.classList.remove('active');
                eventDetailsModalOverlay.setAttribute('aria-hidden', 'true');
                document.body.style.overflow = '';
            }
        };

        if (closeEventDetailsModalBtn) closeEventDetailsModalBtn.addEventListener('click', closeModal);
        if (dismissEventDetailsBtn) dismissEventDetailsBtn.addEventListener('click', closeModal);

        if (eventDetailsModalOverlay) {
            eventDetailsModalOverlay.addEventListener('click', (e) => {
                if (e.target === eventDetailsModalOverlay) closeModal();
            });
        }
    }

    // --------------------------------------------------------------------------
    // 6. Utilities
    // --------------------------------------------------------------------------
    function showToast(message, type = 'success') {
        const container = document.getElementById('toastContainer');
        if (!container) return;

        const isSuccess = type === 'success';
        const iconSvg = isSuccess
            ? `<svg class="toast-icon" width="18" height="18" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/></svg>`
            : `<svg class="toast-icon" width="18" height="18" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"/></svg>`;

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
