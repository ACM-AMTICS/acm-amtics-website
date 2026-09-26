/**
 * ACM AMTICS - My Projects Module Logic
 */

(function () {
    const STORAGE_KEY = 'acm_user_projects_list';
    const PROFILE_KEY = 'acm_user_profile_data';

    const defaultProjects = [
        {
            id: 'proj-1',
            name: 'EcoTrack',
            category: 'Web Development',
            description: 'A comprehensive campus sustainability web platform to help students track and minimize campus energy consumption & carbon footprint with automated telemetry.',
            techStack: ['React', 'Node.js', 'MongoDB', 'Chart.js', 'Tailwind'],
            status: 'Under Review',
            statusKey: 'review',
            statusClass: 'status-badge-review',
            date: '15 Sep 2025',
            image: '/images/projects/ecotrack.jpg',
            github: 'https://github.com/acm-amtics/ecotrack',
            live: 'https://ecotrack.amtics.ac.in',
            team: ['Ayesha Khan (Lead)', 'Rohan Mehta', 'Priya Patel'],
            architecture: 'MERN Stack architecture containerized with Docker, integrating REST APIs and real-time MongoDB aggregation pipelines.'
        },
        {
            id: 'proj-2',
            name: 'LearnMate',
            category: 'Artificial Intelligence',
            description: 'AI-powered study assistant for learners that generates automated course summaries, interactive flashcards, and conceptual quizzes using advanced LLM reasoning.',
            techStack: ['Next.js', 'Python', 'FastAPI', 'Gemini API', 'PostgreSQL'],
            status: 'Featured',
            statusKey: 'featured',
            statusClass: 'status-badge-featured',
            date: '5 Sep 2025',
            image: '/images/projects/learnmate.jpg',
            github: 'https://github.com/acm-amtics/learnmate-ai',
            live: 'https://learnmate.ai',
            team: ['Ayesha Khan (Core ML)', 'Dev Sharma'],
            architecture: 'Next.js frontend with FastAPI microservices querying Google Gemini 1.5 Pro via structured RAG embeddings.'
        },
        {
            id: 'proj-3',
            name: 'AMTICS Navigator',
            category: 'Mobile App',
            description: 'Interactive 3D indoor campus navigation and classroom finder mobile application with pathfinding algorithms and real-time faculty cabin routing.',
            techStack: ['Flutter', 'Dart', 'Firebase', 'Three.js'],
            status: 'Under Review',
            statusKey: 'review',
            statusClass: 'status-badge-review',
            date: '22 Aug 2025',
            image: '/images/projects/amtics-navigator.jpg',
            github: 'https://github.com/acm-amtics/amtics-nav',
            live: 'https://nav.amtics.ac.in',
            team: ['Ayesha Khan', 'Kavya Joshi', 'Harsh Trivedi'],
            architecture: 'Cross-platform Flutter client consuming Firebase Firestore and rendered SVG/3D indoor floor plans.'
        },
        {
            id: 'proj-4',
            name: 'Campus Connect',
            category: 'Web Development',
            description: 'A centralized peer-to-peer student collaboration, project teaming, and lecture notes sharing forum for AMTICS engineering students.',
            techStack: ['ASP.NET Core', 'C#', 'SQL Server', 'Bootstrap 5'],
            status: 'Approved',
            statusKey: 'approved',
            statusClass: 'status-badge-approved',
            date: '10 Aug 2025',
            image: '/images/amtics-building.jpg',
            github: 'https://github.com/acm-amtics/campus-connect',
            live: 'https://connect.amtics.ac.in',
            team: ['Ayesha Khan', 'Sahil Qureshi'],
            architecture: 'ASP.NET Core MVC architecture with Entity Framework Core and Azure Blob Storage for notes attachments.'
        }
    ];

    let projects = [];
    let currentFilter = 'all';
    let searchQuery = '';

    function init() {
        const saved = localStorage.getItem(STORAGE_KEY);
        if (saved) {
            try {
                projects = JSON.parse(saved);
            } catch (e) {
                projects = [...defaultProjects];
            }
        } else {
            projects = [...defaultProjects];
            saveProjects();
        }

        renderProjects();
        updateMetrics();

        // Check if query param ?submit=true is present
        const urlParams = new URLSearchParams(window.location.search);
        if (urlParams.get('submit') === 'true') {
            setTimeout(openNewProjectModal, 150);
        }
    }

    function saveProjects() {
        localStorage.setItem(STORAGE_KEY, JSON.stringify(projects));
        // Also sync to profile state if present
        syncToProfileState();
    }

    function syncToProfileState() {
        const profileRaw = localStorage.getItem(PROFILE_KEY);
        if (profileRaw) {
            try {
                const profileData = JSON.parse(profileRaw);
                profileData.projects = projects.map(p => ({
                    name: p.name,
                    description: p.description,
                    status: p.status,
                    statusClass: p.status === 'Featured' ? 'pill-project-featured' : (p.status === 'Approved' ? 'pill-project-approved' : 'pill-project-review'),
                    date: p.date,
                    image: p.image
                }));
                profileData.stats.projectsSubmitted = projects.length;
                localStorage.setItem(PROFILE_KEY, JSON.stringify(profileData));
            } catch (e) { }
        }
    }

    function renderProjects() {
        const grid = document.getElementById('userProjectsGrid');
        if (!grid) return;

        let filtered = projects.filter(p => {
            const matchesStatus = currentFilter === 'all' || p.statusKey === currentFilter;
            const q = searchQuery.toLowerCase();
            const matchesQuery = !q ||
                p.name.toLowerCase().includes(q) ||
                p.category.toLowerCase().includes(q) ||
                p.description.toLowerCase().includes(q) ||
                p.techStack.some(t => t.toLowerCase().includes(q));
            return matchesStatus && matchesQuery;
        });

        if (filtered.length === 0) {
            grid.innerHTML = `
                <div style="grid-column: 1 / -1; text-align: center; padding: 4rem 1.5rem; background: #FFFFFF; border-radius: 18px; border: 1.5px dashed #E2E8F0;">
                    <svg width="48" height="48" fill="none" viewBox="0 0 24 24" stroke="#94A3B8" style="margin: 0 auto 1rem auto; display: block;">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M3 7v10a2 2 0 002 2h14a2 2 0 002-2V9a2 2 0 00-2-2h-6l-2-2H5a2 2 0 00-2 2z" />
                    </svg>
                    <h3 style="font-size: 1.15rem; font-weight: 700; color: #1E293B;">No matching projects found</h3>
                    <p style="font-size: 0.875rem; color: #64748B; margin-top: 0.35rem;">Try modifying your search query or switching the status filter.</p>
                </div>
            `;
            return;
        }

        grid.innerHTML = filtered.map(p => `
            <div class="project-card">
                <div class="project-card-cover-wrap">
                    <img src="${p.image}" alt="${p.name}" class="project-card-cover-img" onerror="this.src='/images/projects/ecotrack.jpg'" />
                    <span class="project-status-badge ${p.statusClass}">${p.status}</span>
                    <span class="project-category-badge">${p.category}</span>
                </div>
                <div class="project-card-content">
                    <h3 class="project-card-title">${p.name}</h3>
                    <p class="project-card-desc">${p.description}</p>
                    
                    <div class="project-tech-tags">
                        ${p.techStack.map(t => `<span class="tech-tag-pill">${t}</span>`).join('')}
                    </div>

                    <div class="project-card-footer">
                        <span class="project-date-text">
                            <svg width="13" height="13" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
                            </svg>
                            ${p.date}
                        </span>
                        <div class="project-card-actions">
                            ${p.github ? `
                                <a href="${p.github}" target="_blank" rel="noopener noreferrer" class="btn-card-action" title="View Source on GitHub">
                                    <svg width="15" height="15" fill="currentColor" viewBox="0 0 24 24">
                                        <path d="M12 0C5.37 0 0 5.37 0 12c0 5.31 3.435 9.795 8.205 11.385.6.105.825-.255.825-.57 0-.285-.015-1.23-.015-2.235-3.015.555-3.795-.735-4.035-1.41-.135-.345-.72-1.41-1.23-1.695-.42-.225-1.02-.78-.015-.795.945-.015 1.62.87 1.845 1.23 1.08 1.815 2.805 1.305 3.495.99.105-.78.42-1.305.765-1.605-2.67-.3-5.46-1.335-5.46-5.925 0-1.305.465-2.385 1.23-3.225-.12-.3-.54-1.53.12-3.18 0 0 1.005-.315 3.3 1.23.96-.27 1.98-.405 3-.405s2.04.135 3 .405c2.295-1.56 3.3-1.23 3.3-1.23.66 1.65.24 2.88.12 3.18.765.84 1.23 1.905 1.23 3.225 0 4.605-2.805 5.625-5.475 5.925.435.375.81 1.095.81 2.22 0 1.605-.015 2.895-.015 3.3 0 .315.225.69.825.57A12.02 12.02 0 0024 12c0-6.63-5.37-12-12-12z"/>
                                    </svg>
                                </a>
                            ` : ''}
                            ${p.live ? `
                                <a href="${p.live}" target="_blank" rel="noopener noreferrer" class="btn-card-action" title="Open Live Deployment">
                                    <svg width="15" height="15" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 6H6a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-4M14 4h6m0 0v6m0-6L10 14" />
                                    </svg>
                                </a>
                            ` : ''}
                            <button type="button" class="btn-card-details" onclick="openProjectDetails('${p.id}')">
                                Details &rarr;
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        `).join('');
    }

    function updateMetrics() {
        const total = projects.length;
        const featured = projects.filter(p => p.statusKey === 'featured').length;
        const approved = projects.filter(p => p.statusKey === 'approved').length;
        const review = projects.filter(p => p.statusKey === 'review').length;

        const setTxt = (id, val) => {
            const el = document.getElementById(id);
            if (el) el.innerText = val;
        };

        setTxt('totalProjectsStat', total);
        setTxt('featuredProjectsStat', featured);
        setTxt('approvedProjectsStat', approved);
        setTxt('reviewProjectsStat', review);

        setTxt('countAll', total);
        setTxt('countFeatured', featured);
        setTxt('countApproved', approved);
        setTxt('countReview', review);
    }

    // Window globals
    window.filterProjectStatus = function (statusKey, btn) {
        currentFilter = statusKey;
        document.querySelectorAll('.proj-filter-pill').forEach(b => b.classList.remove('active'));
        if (btn) btn.classList.add('active');
        renderProjects();
    };

    window.searchProjects = function () {
        const input = document.getElementById('projectsSearchInput');
        searchQuery = (input ? input.value : '').trim();
        renderProjects();
    };

    // Project Details Modal
    window.openProjectDetails = function (id) {
        const p = projects.find(x => x.id === id);
        if (!p) return;

        const content = document.getElementById('projectDetailsContent');
        if (!content) return;

        content.innerHTML = `
            <div style="position:relative; border-radius:14px; overflow:hidden; margin-bottom:1.5rem; height:200px;">
                <img src="${p.image}" alt="${p.name}" style="width:100%; height:100%; object-fit:cover;" onerror="this.src='/images/projects/ecotrack.jpg'" />
                <span class="project-status-badge ${p.statusClass}" style="position:absolute; top:12px; right:12px;">${p.status}</span>
            </div>

            <h2 style="font-size:1.45rem; font-weight:800; color:#111827; margin-bottom:0.25rem;">${p.name}</h2>
            <p style="font-size:0.875rem; color:#7C3AED; font-weight:600; margin-bottom:1rem;">${p.category} &bull; Submitted ${p.date}</p>

            <div style="display:flex; flex-direction:column; gap:1.25rem; font-size:0.875rem; color:#4B5563;">
                <div>
                    <h4 style="font-size:0.85rem; font-weight:700; color:#111827; text-transform:uppercase; letter-spacing:0.04em; margin-bottom:0.4rem;">Project Abstract</h4>
                    <p style="line-height:1.6;">${p.description}</p>
                </div>

                ${p.architecture ? `
                    <div>
                        <h4 style="font-size:0.85rem; font-weight:700; color:#111827; text-transform:uppercase; letter-spacing:0.04em; margin-bottom:0.4rem;">Architecture & Engineering Highlights</h4>
                        <p style="line-height:1.6;">${p.architecture}</p>
                    </div>
                ` : ''}

                <div>
                    <h4 style="font-size:0.85rem; font-weight:700; color:#111827; text-transform:uppercase; letter-spacing:0.04em; margin-bottom:0.5rem;">Tech Stack & Frameworks</h4>
                    <div style="display:flex; gap:0.4rem; flex-wrap:wrap;">
                        ${p.techStack.map(t => `<span class="tech-tag-pill" style="font-size:0.75rem; padding:0.25rem 0.65rem;">${t}</span>`).join('')}
                    </div>
                </div>

                ${p.team && p.team.length ? `
                    <div>
                        <h4 style="font-size:0.85rem; font-weight:700; color:#111827; text-transform:uppercase; letter-spacing:0.04em; margin-bottom:0.4rem;">Team Contributors</h4>
                        <ul style="padding-left:1.2rem; margin:0; line-height:1.5;">
                            ${p.team.map(m => `<li>${m}</li>`).join('')}
                        </ul>
                    </div>
                ` : ''}

                <div style="display:flex; align-items:center; gap:0.75rem; margin-top:0.75rem; padding-top:1.25rem; border-top:1px solid #E2E8F0;">
                    ${p.github ? `
                        <a href="${p.github}" target="_blank" rel="noopener noreferrer" class="btn-submit-new-project" style="background:#1E293B; font-size:0.85rem; padding:0.6rem 1.1rem; text-decoration:none;">
                            View GitHub Code &rarr;
                        </a>
                    ` : ''}
                    ${p.live ? `
                        <a href="${p.live}" target="_blank" rel="noopener noreferrer" class="btn-submit-new-project" style="font-size:0.85rem; padding:0.6rem 1.1rem; text-decoration:none;">
                            Open Live App &rarr;
                        </a>
                    ` : ''}
                </div>
            </div>
        `;

        const modal = document.getElementById('projectDetailsModal');
        if (modal) {
            modal.classList.add('active');
            document.body.style.overflow = 'hidden';
        }
    };

    window.closeProjectDetailsModal = function () {
        const modal = document.getElementById('projectDetailsModal');
        if (modal) {
            modal.classList.remove('active');
            document.body.style.overflow = '';
        }
    };

    // Submit New Project Modal
    window.openNewProjectModal = function () {
        const modal = document.getElementById('projectSubmitModal');
        if (modal) {
            modal.classList.add('active');
            document.body.style.overflow = 'hidden';
        }
    };

    window.closeProjectSubmitModal = function () {
        const modal = document.getElementById('projectSubmitModal');
        if (modal) {
            modal.classList.remove('active');
            document.body.style.overflow = '';
        }
    };

    window.setProjectCover = function (url) {
        const input = document.getElementById('projImageSelect');
        if (input) input.value = url;
    };

    window.handleProjectSubmit = function (e) {
        e.preventDefault();

        const title = document.getElementById('projTitleInput').value.trim();
        const category = document.getElementById('projCategoryInput').value;
        const stackRaw = document.getElementById('projTechStackInput').value.trim();
        const github = document.getElementById('projGithubInput').value.trim();
        const live = document.getElementById('projLiveInput').value.trim();
        const image = document.getElementById('projImageSelect').value.trim() || '/images/projects/ecotrack.jpg';
        const desc = document.getElementById('projDescInput').value.trim();

        if (!title || !desc) {
            alert('Please fill in the project title and description.');
            return;
        }

        const stackArr = stackRaw.split(',').map(s => s.trim()).filter(s => s.length > 0);

        const newProject = {
            id: 'proj-' + Date.now(),
            name: title,
            category: category,
            description: desc,
            techStack: stackArr.length > 0 ? stackArr : ['Web', 'Cloud'],
            status: 'Under Review',
            statusKey: 'review',
            statusClass: 'status-badge-review',
            date: 'Today',
            image: image,
            github: github || null,
            live: live || null,
            team: ['Ayesha Khan (Submitter)'],
            architecture: 'Newly registered ACM student project queued for faculty evaluation and showcase approval.'
        };

        projects.unshift(newProject);
        saveProjects();
        closeProjectSubmitModal();
        renderProjects();
        updateMetrics();

        alert('Project "' + title + '" successfully submitted for ACM chapter review! 🎉');
    };

    // Keyboard support
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') {
            closeProjectDetailsModal();
            closeProjectSubmitModal();
        }
    });

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
