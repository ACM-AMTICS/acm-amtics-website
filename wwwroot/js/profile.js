/**
 * ACM AMTICS - Profile Management & Event Pass Interactivity
 * Powers My Profile, Dynamic Badges Calculation, Ticket Modal, and Live Editing.
 */

(function () {
    'use strict';

    // Default Profile Data (Matches Screenshots)
    const DEFAULT_PROFILE = {
        name: "Ayesha Khan",
        role: "Member",
        email: "ayesha.k@amtics.ac.in",
        department: "Computer Science & Engineering",
        academicYear: "3rd Year (2025 - 2026)",
        yearShort: "3rd Year • CSE",
        college: "AMTICS, Gandhinagar",
        memberSince: "Aug 2024",
        ticketId: "#AMTICS-2024-087",
        bio: "Passionate about design, technology and community building. Excited to learn, collaborate and create meaningful impact with ACM AMTICS.",
        quote: "Ideas grow when shared.",
        avatar: "/images/avatars/ayesha-khan.jpg",
        skills: ["UI/UX Design", "Frontend Development", "React.js", "Figma", "Python", "Community Outreach"],
        ideasShared: 2,
        communityInitiatives: 3,
        socials: {
            github: "https://github.com",
            linkedin: "https://linkedin.com",
            twitter: "https://twitter.com",
            website: "https://acmamtics.org"
        }
    };

    // Default Events Attended (12 Events)
    const DEFAULT_EVENTS = [
        {
            id: "evt-01",
            name: "UI/UX Design Workshop",
            date: "14 Dec 2025",
            type: "Workshop",
            tagClass: "pill-tag-workshop",
            image: "/images/events/ui-ux-workshop.jpg",
            venue: "Lab 3, AMTICS",
            status: "Attended"
        },
        {
            id: "evt-02",
            name: "Introduction to AI/ML",
            date: "25 Sep 2025",
            type: "Seminar",
            tagClass: "pill-tag-seminar",
            image: "/images/events/ai-ml-intro.jpg",
            venue: "Auditorium",
            status: "Attended"
        },
        {
            id: "evt-03",
            name: "ACM Hackathon 2025",
            date: "10 Oct 2025",
            type: "Competition",
            tagClass: "pill-tag-competition",
            image: "/images/events/acm-hackathon.jpg",
            venue: "Main Hall",
            status: "Attended"
        },
        {
            id: "evt-04",
            name: "Open Source & GSoC Talk",
            date: "5 Sep 2025",
            type: "Workshop",
            tagClass: "pill-tag-workshop",
            image: "/images/events/open-source-session.jpg",
            venue: "Seminar Hall 2",
            status: "Attended"
        },
        {
            id: "evt-05",
            name: "Flutter App Development",
            date: "18 Aug 2025",
            type: "Workshop",
            tagClass: "pill-tag-workshop",
            image: "/images/events/flutter-workshop.jpg",
            venue: "Lab 1",
            status: "Attended"
        },
        {
            id: "evt-06",
            name: "Web Dev Bootcamp 2025",
            date: "20 Jul 2025",
            type: "Workshop",
            tagClass: "pill-tag-workshop",
            image: "/images/events/web-dev-workshop.jpg",
            venue: "Online",
            status: "Attended"
        },
        {
            id: "evt-07",
            name: "Career Guidance Session",
            date: "15 Jun 2025",
            type: "Seminar",
            tagClass: "pill-tag-seminar",
            image: "/images/events/career-guidance.jpg",
            venue: "Auditorium",
            status: "Attended"
        },
        {
            id: "evt-08",
            name: "Tech for Social Good",
            date: "2 May 2025",
            type: "Talk",
            tagClass: "pill-tag-talk",
            image: "/images/events/tech-social-good.jpg",
            venue: "Seminar Room A",
            status: "Attended"
        },
        {
            id: "evt-09",
            name: "Cloud Computing 101",
            date: "10 Apr 2025",
            type: "Workshop",
            tagClass: "pill-tag-workshop",
            image: "/images/events/ui-ux-workshop.jpg",
            venue: "Lab 2",
            status: "Attended"
        },
        {
            id: "evt-10",
            name: "Git & GitHub Mastery",
            date: "20 Mar 2025",
            type: "Workshop",
            tagClass: "pill-tag-workshop",
            image: "/images/events/open-source-session.jpg",
            venue: "Lab 4",
            status: "Attended"
        },
        {
            id: "evt-11",
            name: "CodeSprint Algo Fest",
            date: "15 Feb 2025",
            type: "Competition",
            tagClass: "pill-tag-competition",
            image: "/images/events/acm-hackathon.jpg",
            venue: "Computer Center",
            status: "Attended"
        },
        {
            id: "evt-12",
            name: "ACM Orientation 2024",
            date: "10 Aug 2024",
            type: "Talk",
            tagClass: "pill-tag-talk",
            image: "/images/events/career-guidance.jpg",
            venue: "Main Auditorium",
            status: "Attended"
        }
    ];

    // Default Submitted Projects (4 Projects)
    const DEFAULT_PROJECTS = [
        {
            id: "proj-01",
            name: "EcoTrack",
            description: "A web platform to help students track their carbon footprint",
            status: "Under Review",
            statusClass: "pill-project-review",
            date: "15 Sep 2025",
            image: "/images/projects/ecotrack.jpg",
            github: "https://github.com/ayesha/ecotrack"
        },
        {
            id: "proj-02",
            name: "LearnMate",
            description: "AI powered study assistant for learners.",
            status: "Featured",
            statusClass: "pill-project-featured",
            date: "5 Sep 2025",
            image: "/images/projects/learnmate.jpg",
            github: "https://github.com/ayesha/learnmate"
        },
        {
            id: "proj-03",
            name: "AMTICS Navigator",
            description: "A mobile app to help students navigate the AMTICS campus.",
            status: "Under Review",
            statusClass: "pill-project-review",
            date: "22 Aug 2025",
            image: "/images/projects/amtics-navigator.jpg",
            github: "https://github.com/ayesha/amtics-navigator"
        },
        {
            id: "proj-04",
            name: "Campus Connect",
            description: "A platform to connect students and share resources.",
            status: "Approved",
            statusClass: "pill-project-approved",
            date: "10 Aug 2025",
            image: "/images/projects/campus-connect.jpg",
            github: "https://github.com/ayesha/campus-connect"
        }
    ];

    // Default Coordinator Roles (2 Roles)
    const DEFAULT_ROLES = [
        {
            id: "role-01",
            eventName: "UI/UX Workshop 2025",
            roleTitle: "Design & Outreach Coordinator",
            date: "14 Dec 2025",
            status: "Current",
            statusClass: "role-badge-current",
            image: "/images/events/ui-ux-workshop.jpg"
        },
        {
            id: "role-02",
            eventName: "ACM Hackathon 2025",
            roleTitle: "Logistics Coordinator",
            date: "10 Oct 2025",
            status: "Past",
            statusClass: "role-badge-past",
            image: "/images/events/acm-hackathon.jpg"
        }
    ];

    // Badge Definitions with Dynamic Unlocking Criteria
    const BADGE_DEFINITIONS = [
        {
            id: "active-member",
            title: "Active Member",
            description: "Participated in 5+ events",
            fullDesc: "Awarded to members who actively attend at least 5 ACM AMTICS workshops, seminars, or competitions.",
            colorType: "purple",
            grad1: "#8B5CF6",
            grad2: "#6D28D9",
            icon: `<svg width="22" height="22" fill="currentColor" viewBox="0 0 24 24"><path d="M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z"/></svg>`,
            criteria: (state) => state.events.length >= 5,
            progress: (state) => `${state.events.length} / 5 events attended`
        },
        {
            id: "event-enthusiast",
            title: "Event Enthusiast",
            description: "Attended 10+ events",
            fullDesc: "Dedicated member who has attended 10 or more student chapter events and actively contributed to technical gatherings.",
            colorType: "green",
            grad1: "#10B981",
            grad2: "#059669",
            icon: `<svg width="22" height="22" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 20h5v-2a3 3 0 00-5-3.512M9 20H4v-2a3 3 0 015-3.512M12 11a4 4 0 100-8 4 4 0 000 8zm0 2a7 7 0 00-7 7h14a7 7 0 00-7-7z" /></svg>`,
            criteria: (state) => state.events.length >= 10,
            progress: (state) => `${state.events.length} / 10 events attended`
        },
        {
            id: "project-contributor",
            title: "Project Contributor",
            description: "Submitted 3+ projects",
            fullDesc: "Recognizes hands-on developers who have built and submitted 3 or more technical projects to ACM AMTICS.",
            colorType: "orange",
            grad1: "#F59E0B",
            grad2: "#D97706",
            icon: `<svg width="22" height="22" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M10 20l4-16m4 4l4 4-4 4M6 16l-4-4 4-4" /></svg>`,
            criteria: (state) => state.projects.length >= 3,
            progress: (state) => `${state.projects.length} / 3 projects submitted`
        },
        {
            id: "idea-sharer",
            title: "Idea Sharer",
            description: "Shared awesome ideas",
            fullDesc: "Awarded for pitching impactful workshop topics, hackathon themes, or open source ideas to the chapter.",
            colorType: "cyan",
            grad1: "#06B6D4",
            grad2: "#0284C7",
            icon: `<svg width="22" height="22" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9.663 17h4.673M12 3v1m6.364 1.636l-.707.707M21 12h-1M4 12H3m3.343-5.657l-.707-.707m2.828 9.9a5 5 0 117.072 0l-.548.547A3.374 3.374 0 0014 18.469V19a2 2 0 11-4 0v-.531c0-.895-.356-1.754-.988-2.386l-.548-.547z" /></svg>`,
            criteria: (state) => (state.profile.ideasShared || 0) >= 1,
            progress: (state) => `${state.profile.ideasShared || 0} / 1 idea pitched`
        },
        {
            id: "community-builder",
            title: "Community Builder",
            description: "Contributed to community initiatives",
            fullDesc: "Honors members who volunteer, mentor junior students, or help build a collaborative atmosphere across ACM AMTICS.",
            colorType: "pink",
            grad1: "#EC4899",
            grad2: "#BE185D",
            icon: `<svg width="22" height="22" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4.318 6.318a4.5 4.5 0 000 6.364L12 20.364l7.682-7.682a4.5 4.5 0 00-6.364-6.364L12 7.636l-1.318-1.318a4.5 4.5 0 00-6.364 0z" /></svg>`,
            criteria: (state) => (state.profile.communityInitiatives || 0) >= 1,
            progress: (state) => `${state.profile.communityInitiatives || 0} / 1 community contribution`
        },
        {
            id: "leadership",
            title: "Leadership",
            description: "Took up a core role in events",
            fullDesc: "Demonstrated exemplary leadership by taking on a coordinator or executive role in organizing chapter events.",
            colorType: "slate",
            grad1: "#64748B",
            grad2: "#475569",
            icon: `<svg width="22" height="22" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" /></svg>`,
            criteria: (state) => state.roles.length >= 1,
            progress: (state) => `${state.roles.length} / 1 coordinator role served`
        }
    ];

    // Local Storage State Loader
    function loadState() {
        let profile = DEFAULT_PROFILE;
        let events = DEFAULT_EVENTS;
        let projects = DEFAULT_PROJECTS;
        let roles = DEFAULT_ROLES;

        try {
            const savedProfile = localStorage.getItem('acm_user_profile_data');
            if (savedProfile) profile = Object.assign({}, DEFAULT_PROFILE, JSON.parse(savedProfile));

            const savedEvents = localStorage.getItem('acm_user_events_data');
            if (savedEvents) events = JSON.parse(savedEvents);

            const savedProjects = localStorage.getItem('acm_user_projects_data');
            if (savedProjects) projects = JSON.parse(savedProjects);

            const savedRoles = localStorage.getItem('acm_user_roles_data');
            if (savedRoles) roles = JSON.parse(savedRoles);
        } catch (e) {
            console.warn('Could not load profile state from localStorage', e);
        }

        return { profile, events, projects, roles };
    }

    function saveState(state) {
        try {
            localStorage.setItem('acm_user_profile_data', JSON.stringify(state.profile));
            localStorage.setItem('acm_user_events_data', JSON.stringify(state.events));
            localStorage.setItem('acm_user_projects_data', JSON.stringify(state.projects));
            localStorage.setItem('acm_user_roles_data', JSON.stringify(state.roles));
        } catch (e) {
            console.warn('Could not save state to localStorage', e);
        }
    }

    const state = loadState();

    // Helper: Compute Badges Earned
    function getBadgesStatus() {
        return BADGE_DEFINITIONS.map(badge => {
            const isEarned = badge.criteria(state);
            return {
                ...badge,
                earned: isEarned,
                progressText: badge.progress(state)
            };
        });
    }

    // Helper: Generate Clean Hexagon SVG with Gradient and Icon
    function renderBadgeHexagon(badge) {
        const isLocked = !badge.earned;
        const gradId = `badge-grad-${badge.id}`;
        
        return `
            <div class="badge-shape-wrap">
                <svg class="badge-hex-svg" viewBox="0 0 100 100" fill="none" xmlns="http://www.w3.org/2000/svg">
                    <defs>
                        <linearGradient id="${gradId}" x1="0%" y1="0%" x2="100%" y2="100%">
                            <stop offset="0%" stop-color="${isLocked ? '#94A3B8' : badge.grad1}" />
                            <stop offset="100%" stop-color="${isLocked ? '#64748B' : badge.grad2}" />
                        </linearGradient>
                    </defs>
                    <polygon points="50,4 90,26 90,74 50,96 10,74 10,26" fill="url(#${gradId})" />
                </svg>
                <div class="badge-icon-overlay">
                    ${badge.icon}
                </div>
            </div>
        `;
    }

    // DOM Hydration & Updates
    function updateProfileUI() {
        const badges = getBadgesStatus();
        const earnedCount = badges.filter(b => b.earned).length;

        // Stat Numbers
        const statBadgesNum = document.getElementById('statBadgesNum');
        if (statBadgesNum) statBadgesNum.textContent = earnedCount;

        const statEventsNum = document.getElementById('statEventsNum');
        if (statEventsNum) statEventsNum.textContent = state.events.length;

        const statProjectsNum = document.getElementById('statProjectsNum');
        if (statProjectsNum) statProjectsNum.textContent = state.projects.length;

        const statRolesNum = document.getElementById('statRolesNum');
        if (statRolesNum) statRolesNum.textContent = state.roles.length;

        // Profile Card Texts
        const nameElements = document.querySelectorAll('.user-profile-name');
        nameElements.forEach(el => el.textContent = state.profile.name);

        const emailElements = document.querySelectorAll('.user-profile-email');
        emailElements.forEach(el => el.textContent = state.profile.email);

        const deptElements = document.querySelectorAll('.user-profile-dept');
        deptElements.forEach(el => el.textContent = `${state.profile.academicYear.split(' ')[0]} ${state.profile.academicYear.split(' ')[1]} • ${state.profile.department}`);

        const bioElements = document.querySelectorAll('.user-profile-bio');
        bioElements.forEach(el => el.textContent = state.profile.bio);

        const quoteElements = document.querySelectorAll('.user-profile-quote');
        quoteElements.forEach(el => el.textContent = state.profile.quote);

        const collegeElements = document.querySelectorAll('.user-profile-college');
        collegeElements.forEach(el => el.textContent = state.profile.college);

        const yearElements = document.querySelectorAll('.user-profile-year');
        yearElements.forEach(el => el.textContent = state.profile.academicYear);

        const avatarElements = document.querySelectorAll('.user-profile-avatar');
        avatarElements.forEach(el => {
            el.src = state.profile.avatar;
        });

        // Topbar User Info
        const topbarName = document.querySelector('.user-header-name');
        if (topbarName) topbarName.textContent = state.profile.name;
        const topbarSub = document.querySelector('.user-header-sub');
        if (topbarSub) topbarSub.textContent = state.profile.yearShort || "3rd Year • CSE";
        const topbarAvatar = document.querySelector('.user-header-avatar');
        if (topbarAvatar) topbarAvatar.src = state.profile.avatar;

        // Render Badges Row
        renderBadges(badges);

        // Render Recent Events
        renderRecentEvents();

        // Render Submitted Projects
        renderSubmittedProjects();

        // Render Coordinator Roles
        renderCoordinatorRoles();

        // Update Ticket Details
        updateTicketData();
    }

    // Render Badges
    function renderBadges(badges) {
        const badgesContainer = document.getElementById('profileBadgesContainer');
        if (!badgesContainer) return;

        badgesContainer.innerHTML = '';
        badges.forEach(badge => {
            const card = document.createElement('div');
            card.className = `badge-item-card ${badge.earned ? 'badge-unlocked' : 'badge-locked'}`;
            card.setAttribute('title', `${badge.title}: ${badge.description}`);

            card.innerHTML = `
                ${renderBadgeHexagon(badge)}
                <div class="badge-title-text">${badge.title}</div>
                <div class="badge-desc-text">${badge.description}</div>
                <span class="badge-status-pill ${badge.earned ? 'badge-status-unlocked' : 'badge-status-locked'}">
                    ${badge.earned ? 'Earned ✓' : 'Locked'}
                </span>
            `;

            card.addEventListener('click', () => openBadgeDetailModal(badge));
            badgesContainer.appendChild(card);
        });
    }

    // Render Recent Events
    function renderRecentEvents(limit = 4) {
        const list = document.getElementById('recentEventsList');
        if (!list) return;

        list.innerHTML = '';
        const displayItems = state.events.slice(0, limit);

        displayItems.forEach(evt => {
            const row = document.createElement('div');
            row.className = 'profile-event-row';
            row.innerHTML = `
                <div class="profile-event-left">
                    <img src="${evt.image}" alt="${evt.name}" class="profile-item-thumb" onerror="this.src='/images/events/ui-ux-workshop.jpg'" />
                    <div class="profile-item-info">
                        <div class="profile-item-name">${evt.name}</div>
                        <div class="profile-item-meta">
                            <svg width="12" height="12" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
                            </svg>
                            ${evt.date}
                        </div>
                    </div>
                </div>
                <div class="profile-event-right">
                    <span class="pill-tag ${evt.tagClass}">${evt.type}</span>
                    <span class="pill-status-attended">
                        <svg width="12" height="12" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M5 13l4 4L19 7" />
                        </svg>
                        Attended
                    </span>
                </div>
            `;
            list.appendChild(row);
        });
    }

    // Render Submitted Projects
    function renderSubmittedProjects(limit = 4) {
        const list = document.getElementById('submittedProjectsList');
        if (!list) return;

        list.innerHTML = '';
        const displayItems = state.projects.slice(0, limit);

        displayItems.forEach(proj => {
            const row = document.createElement('div');
            row.className = 'profile-project-row';
            row.innerHTML = `
                <div class="profile-event-left">
                    <img src="${proj.image}" alt="${proj.name}" class="profile-item-thumb" onerror="this.src='/images/projects/ecotrack.jpg'" />
                    <div class="profile-item-info">
                        <div class="profile-item-name">${proj.name}</div>
                        <div class="profile-project-desc">${proj.description}</div>
                    </div>
                </div>
                <div class="profile-event-right">
                    <div style="display:flex; flex-direction:column; align-items:flex-end; gap:0.25rem;">
                        <span class="${proj.statusClass}">${proj.status}</span>
                        <span style="font-size:0.7rem; color:#9CA3AF;">${proj.date}</span>
                    </div>
                    <button class="profile-more-btn" title="Project Options" onclick="alert('${proj.name}\\nStatus: ${proj.status}\\nDate: ${proj.date}')">
                        <svg width="16" height="16" fill="currentColor" viewBox="0 0 24 24">
                            <path d="M12 8c1.1 0 2-.9 2-2s-.9-2-2-2-2 .9-2 2 .9 2 2 2zm0 2c-1.1 0-2 .9-2 2s.9 2 2 2 2-.9 2-2-.9-2-2-2zm0 6c-1.1 0-2 .9-2 2s.9 2 2 2 2-.9 2-2-.9-2-2-2z"/>
                        </svg>
                    </button>
                </div>
            `;
            list.appendChild(row);
        });
    }

    // Render Coordinator Roles
    function renderCoordinatorRoles() {
        const list = document.getElementById('coordinatorRolesList');
        if (!list) return;

        list.innerHTML = '';
        state.roles.forEach(role => {
            const row = document.createElement('div');
            row.className = 'profile-event-row';
            row.innerHTML = `
                <div class="profile-event-left">
                    <img src="${role.image}" alt="${role.eventName}" class="profile-item-thumb" onerror="this.src='/images/events/ui-ux-workshop.jpg'" />
                    <div class="profile-item-info">
                        <div class="profile-item-name">${role.eventName}</div>
                        <div class="profile-item-meta" style="font-weight:600; color:#4B5563;">
                            ${role.roleTitle}
                        </div>
                        <div class="profile-item-meta" style="font-size:0.7rem;">
                            <svg width="11" height="11" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
                            </svg>
                            ${role.date}
                        </div>
                    </div>
                </div>
                <div class="profile-event-right">
                    <span class="${role.statusClass}">${role.status}</span>
                    <button class="profile-more-btn" title="Role details">
                        <svg width="16" height="16" fill="currentColor" viewBox="0 0 24 24">
                            <path d="M12 8c1.1 0 2-.9 2-2s-.9-2-2-2-2 .9-2 2 .9 2 2 2zm0 2c-1.1 0-2 .9-2 2s.9 2 2 2 2-.9 2-2-.9-2-2-2zm0 6c-1.1 0-2 .9-2 2s.9 2 2 2 2-.9 2-2-.9-2-2-2z"/>
                        </svg>
                    </button>
                </div>
            `;
            list.appendChild(row);
        });
    }

    // Update Event Ticket data
    function updateTicketData() {
        const ticketName = document.getElementById('ticketHolderName');
        if (ticketName) ticketName.textContent = state.profile.name;

        const ticketDept = document.getElementById('ticketHolderDept');
        if (ticketDept) ticketDept.textContent = `3rd Year • ${state.profile.department}`;

        const ticketCode = document.getElementById('ticketCodeDisplay');
        if (ticketCode) ticketCode.textContent = state.profile.ticketId || '#AMTICS-2024-087';

        const ticketMemberSince = document.getElementById('ticketMemberSince');
        if (ticketMemberSince) ticketMemberSince.textContent = state.profile.memberSince || 'Aug 2024';
    }

    // Modal Control: Open Event Ticket (Image 2)
    window.openEventTicketModal = function () {
        const modal = document.getElementById('eventTicketModal');
        if (modal) {
            updateTicketData();
            modal.classList.add('active');
            document.body.style.overflow = 'hidden';
        }
    };

    window.closeEventTicketModal = function () {
        const modal = document.getElementById('eventTicketModal');
        if (modal) {
            modal.classList.remove('active');
            document.body.style.overflow = '';
        }
    };

    // Modal Control: Edit Profile
    window.openEditProfileModal = function () {
        const modal = document.getElementById('editProfileModal');
        if (!modal) return;

        // Prefill inputs
        document.getElementById('editNameInput').value = state.profile.name;
        document.getElementById('editEmailInput').value = state.profile.email;
        document.getElementById('editDeptInput').value = state.profile.department;
        document.getElementById('editYearInput').value = state.profile.academicYear;
        document.getElementById('editCollegeInput').value = state.profile.college;
        document.getElementById('editBioInput').value = state.profile.bio;
        document.getElementById('editQuoteInput').value = state.profile.quote;

        modal.classList.add('active');
        document.body.style.overflow = 'hidden';
    };

    window.closeEditProfileModal = function () {
        const modal = document.getElementById('editProfileModal');
        if (modal) {
            modal.classList.remove('active');
            document.body.style.overflow = '';
        }
    };

    window.saveProfileChanges = function (e) {
        if (e) e.preventDefault();

        state.profile.name = document.getElementById('editNameInput').value.trim() || state.profile.name;
        state.profile.email = document.getElementById('editEmailInput').value.trim() || state.profile.email;
        state.profile.department = document.getElementById('editDeptInput').value.trim() || state.profile.department;
        state.profile.academicYear = document.getElementById('editYearInput').value.trim() || state.profile.academicYear;
        state.profile.college = document.getElementById('editCollegeInput').value.trim() || state.profile.college;
        state.profile.bio = document.getElementById('editBioInput').value.trim() || state.profile.bio;
        state.profile.quote = document.getElementById('editQuoteInput').value.trim() || state.profile.quote;

        saveState(state);
        updateProfileUI();
        closeEditProfileModal();

        showToast("Profile updated successfully!");
    };

    // Modal Control: Submit Project
    window.openSubmitProjectModal = function () {
        const modal = document.getElementById('submitProjectModal');
        if (modal) {
            modal.classList.add('active');
            document.body.style.overflow = 'hidden';
        }
    };

    window.closeSubmitProjectModal = function () {
        const modal = document.getElementById('submitProjectModal');
        if (modal) {
            modal.classList.remove('active');
            document.body.style.overflow = '';
        }
    };

    window.handleProjectSubmit = function (e) {
        if (e) e.preventDefault();

        const name = document.getElementById('projTitleInput').value.trim();
        const desc = document.getElementById('projDescInput').value.trim();
        const link = document.getElementById('projLinkInput').value.trim();

        if (!name || !desc) {
            alert("Please provide both project title and description.");
            return;
        }

        const newProject = {
            id: `proj-${Date.now()}`,
            name: name,
            description: desc,
            status: "Under Review",
            statusClass: "pill-project-review",
            date: "Today",
            image: "/images/projects/ecotrack.jpg",
            github: link || "https://github.com"
        };

        state.projects.unshift(newProject);
        saveState(state);
        updateProfileUI();
        closeSubmitProjectModal();

        showToast("Project submitted for review!");
    };

    // Badge Detail Modal
    function openBadgeDetailModal(badge) {
        const modal = document.getElementById('badgeDetailModal');
        if (!modal) return;

        document.getElementById('badgeDetailTitle').textContent = badge.title;
        document.getElementById('badgeDetailDesc').textContent = badge.fullDesc;
        document.getElementById('badgeDetailProgress').textContent = badge.progressText;
        
        const statusEl = document.getElementById('badgeDetailStatus');
        if (badge.earned) {
            statusEl.className = "badge-status-pill badge-status-unlocked";
            statusEl.textContent = "Earned & Active ✓";
        } else {
            statusEl.className = "badge-status-pill badge-status-locked";
            statusEl.textContent = "Locked (Criteria in Progress)";
        }

        const iconContainer = document.getElementById('badgeDetailIconWrap');
        iconContainer.innerHTML = renderBadgeHexagon(badge);

        modal.classList.add('active');
        document.body.style.overflow = 'hidden';
    }

    window.closeBadgeDetailModal = function () {
        const modal = document.getElementById('badgeDetailModal');
        if (modal) {
            modal.classList.remove('active');
            document.body.style.overflow = '';
        }
    };

    // Tab Switching
    window.switchProfileTab = function (tabName, triggerBtn) {
        // Update tab buttons
        const tabButtons = document.querySelectorAll('.profile-tab-item');
        tabButtons.forEach(btn => btn.classList.remove('active'));
        if (triggerBtn) triggerBtn.classList.add('active');

        // Update tab sections
        const sections = document.querySelectorAll('.profile-tab-section');
        sections.forEach(sec => sec.style.display = 'none');

        const targetSection = document.getElementById(`tabSection-${tabName}`);
        if (targetSection) {
            targetSection.style.display = 'block';
        }
    };

    // Toast helper
    function showToast(msg) {
        const toast = document.createElement('div');
        toast.style.position = 'fixed';
        toast.style.bottom = '24px';
        toast.style.right = '24px';
        toast.style.background = '#111827';
        toast.style.color = '#FFFFFF';
        toast.style.padding = '0.75rem 1.25rem';
        toast.style.borderRadius = '10px';
        toast.style.fontSize = '0.875rem';
        toast.style.fontWeight = '600';
        toast.style.boxShadow = '0 10px 25px rgba(0,0,0,0.2)';
        toast.style.zIndex = '99999';
        toast.style.display = 'flex';
        toast.style.alignItems = 'center';
        toast.style.gap = '0.5rem';
        toast.innerHTML = `
            <svg width="18" height="18" fill="none" viewBox="0 0 24 24" stroke="#10B981">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M5 13l4 4L19 7" />
            </svg>
            ${msg}
        `;
        document.body.appendChild(toast);
        setTimeout(() => {
            toast.style.opacity = '0';
            toast.style.transition = 'opacity 0.3s ease';
            setTimeout(() => toast.remove(), 300);
        }, 3000);
    }

    // Avatar upload/change helper
    window.triggerAvatarUpload = function () {
        const input = document.createElement('input');
        input.type = 'file';
        input.accept = 'image/*';
        input.onchange = function (e) {
            const file = e.target.files[0];
            if (file) {
                const reader = new FileReader();
                reader.onload = function (event) {
                    state.profile.avatar = event.target.result;
                    saveState(state);
                    updateProfileUI();
                    showToast("Profile photo updated!");
                };
                reader.readAsDataURL(file);
            }
        };
        input.click();
    };

    // Print / Download Ticket
    window.printEventTicket = function () {
        window.print();
    };

    // Search Filter
    window.handleGlobalProfileSearch = function (query) {
        if (!query) {
            renderRecentEvents(4);
            renderSubmittedProjects(4);
            return;
        }
        const q = query.toLowerCase();
        
        // Filter events
        const filteredEvents = state.events.filter(e => e.name.toLowerCase().includes(q) || e.type.toLowerCase().includes(q));
        const list = document.getElementById('recentEventsList');
        if (list) {
            list.innerHTML = '';
            if (filteredEvents.length === 0) {
                list.innerHTML = `<p style="font-size:0.8125rem; color:#9CA3AF; padding:1rem;">No events found matching "${query}".</p>`;
            } else {
                filteredEvents.forEach(evt => {
                    const row = document.createElement('div');
                    row.className = 'profile-event-row';
                    row.innerHTML = `
                        <div class="profile-event-left">
                            <img src="${evt.image}" alt="${evt.name}" class="profile-item-thumb" onerror="this.src='/images/events/ui-ux-workshop.jpg'" />
                            <div class="profile-item-info">
                                <div class="profile-item-name">${evt.name}</div>
                                <div class="profile-item-meta">${evt.date}</div>
                            </div>
                        </div>
                        <div class="profile-event-right">
                            <span class="pill-tag ${evt.tagClass}">${evt.type}</span>
                            <span class="pill-status-attended">Attended</span>
                        </div>
                    `;
                    list.appendChild(row);
                });
            }
        }
    };

    // Initialize on DOM Ready
    document.addEventListener('DOMContentLoaded', () => {
        updateProfileUI();

        // Listen for topbar search
        const topSearch = document.getElementById('globalTopSearch');
        if (topSearch) {
            topSearch.addEventListener('input', (e) => {
                handleGlobalProfileSearch(e.target.value);
            });
        }
    });

    // Expose state globally for debugging & testing
    window.acmProfileState = state;
    window.refreshProfileUI = updateProfileUI;

})();
