document.addEventListener("DOMContentLoaded", function () {
    fetch('/data/homeData.json')
        .then(response => {
            if (!response.ok) {
                throw new Error('Failed to load homeData.json');
            }
            return response.json();
        })
        .then(data => {
            renderHero(data.hero);
            renderStats(data.stats);
            renderAbout(data.about);
            renderFocus(data.whatWeDo);
            renderEvents(data.eventsSection);
            renderProjects(data.projectsSection);
            renderWhyJoin(data.whyJoinSection);
            renderGallery(data.gallerySection);
            renderCTA(data.ctaSection);
        })
        .catch(err => {
            console.error('Error hydrating landing page:', err);
        });
});

function renderHero(hero) {
    if (!hero) return;
    setText('hero-badge-text', hero.badge);
    setText('hero-title-line1', hero.titleLine1);
    setText('hero-title-line2', hero.titleLine2);
    setText('hero-subtitle', hero.subtitle);
    setText('hero-description', hero.description);
    setText('hero-community-text', hero.communityStatsText);
}

function renderStats(stats) {
    if (!stats || !stats.length) return;
    const container = document.getElementById('stats-container');
    if (!container) return;

    const iconMap = {
        users: '<svg fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z"/></svg>',
        calendar: '<svg fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z"/></svg>',
        folder: '<svg fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 7v10a2 2 0 002 2h14a2 2 0 002-2V9a2 2 0 00-2-2h-6l-2-2H5a2 2 0 00-2 2z"/></svg>',
        trophy: '<svg fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 3v4M3 5h4M6 17v4m-2-2h4m5-16l2.286 6.857L21 12l-5.714 2.143L13 21l-2.286-6.857L5 12l5.714-2.143L13 3z"/></svg>'
    };

    container.innerHTML = stats.map(st => `
        <div class="stat-card">
            <div class="stat-icon-wrapper">
                ${iconMap[st.icon] || iconMap.users}
            </div>
            <div class="stat-info">
                <span class="stat-number">${escapeHtml(st.number)}</span>
                <span class="stat-label">${escapeHtml(st.label)}</span>
            </div>
        </div>
    `).join('');
}

function renderAbout(about) {
    if (!about) return;
    setText('about-tag', about.sectionTag);
    setText('about-title', about.title);
    setText('about-description', about.description);
}

function renderFocus(focus) {
    if (!focus) return;
    setText('focus-tag', focus.sectionTag);
    setText('focus-title', focus.title);
}

function renderEvents(eventsSection) {
    if (!eventsSection) return;
    setText('events-tag', eventsSection.sectionTag);
    setText('events-title', eventsSection.title);
    setText('events-subtitle', eventsSection.subtitle);

    const container = document.getElementById('events-grid-container');
    if (!container || !eventsSection.events) return;

    container.innerHTML = eventsSection.events.map(ev => `
        <div class="event-card">
            <div class="card-img-holder">
                <div class="card-img-placeholder">
                    <svg fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z"/></svg>
                </div>
            </div>
            <div class="event-card-body">
                <div class="event-meta-row">
                    <span class="event-badge badge-blue">${escapeHtml(ev.badge)}</span>
                    <span class="event-date">
                        <svg class="meta-icon" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z"/></svg>
                        ${escapeHtml(ev.date)}
                    </span>
                </div>
                <h3 class="event-title">${escapeHtml(ev.title)}</h3>
                <p class="event-desc">${escapeHtml(ev.description)}</p>
                <div class="event-card-footer">
                    <button class="btn-card-circle" aria-label="View Details">
                        <svg fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M14 5l7 7m0 0l-7 7m7-7H3"/></svg>
                    </button>
                </div>
            </div>
        </div>
    `).join('');
}

function renderProjects(projectsSection) {
    if (!projectsSection) return;
    setText('projects-tag', projectsSection.sectionTag);
    setText('projects-title', projectsSection.title);
    setText('projects-subtitle', projectsSection.subtitle);

    const container = document.getElementById('projects-grid-container');
    if (!container || !projectsSection.projects) return;

    container.innerHTML = projectsSection.projects.map(proj => `
        <div class="project-card">
            <div class="project-img-holder">
                <div class="project-img-preview">
                    <div class="preview-header-dots"><span></span><span></span><span></span></div>
                    <div class="preview-layout-mock"></div>
                </div>
            </div>
            <div class="project-card-body">
                <h3 class="project-title">${escapeHtml(proj.title)}</h3>
                <p class="project-desc">${escapeHtml(proj.description)}</p>
                <div class="project-tags-row">
                    ${(proj.tags || []).map(t => `<span class="project-tag-pill">${escapeHtml(t)}</span>`).join('')}
                </div>
                <div class="project-card-footer">
                    <button class="btn-card-circle" aria-label="View Project">
                        <svg fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M14 5l7 7m0 0l-7 7m7-7H3"/></svg>
                    </button>
                </div>
            </div>
        </div>
    `).join('');
}

function renderWhyJoin(whyJoin) {
    if (!whyJoin) return;
    setText('whyjoin-tag', whyJoin.sectionTag);
    setText('whyjoin-title', whyJoin.title);
    setText('whyjoin-desc', whyJoin.description);

    const imgContainer = document.getElementById('whyjoin-img-container');
    if (imgContainer) {
        imgContainer.innerHTML = `
            <div class="why-join-banner-mock">
                <div class="banner-overlay"></div>
                <div class="banner-text">ACM AMTICS Team Photo</div>
            </div>
        `;
    }
}

function renderGallery(gallerySection) {
    if (!gallerySection) return;
    setText('gallery-tag', gallerySection.sectionTag);
    setText('gallery-title', gallerySection.title);

    const container = document.getElementById('gallery-grid-container');
    if (!container || !gallerySection.images) return;

    container.innerHTML = gallerySection.images.map((img, idx) => `
        <div class="gallery-item item-box-${(idx % 4) + 1}">
            <div class="gallery-img-placeholder">
                <span class="gallery-label">${escapeHtml(img.alt || 'ACM Moment')}</span>
            </div>
        </div>
    `).join('');
}

function renderCTA(cta) {
    if (!cta) return;
    setText('cta-tag', cta.sectionTag);
    setText('cta-title', cta.title);
    setText('cta-subtitle', cta.subtitle);
}

function setText(id, text) {
    if (!text) return;
    const el = document.getElementById(id);
    if (el) el.textContent = text;
}

function escapeHtml(str) {
    if (!str) return '';
    return String(str)
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;")
        .replace(/'/g, "&#039;");
}
