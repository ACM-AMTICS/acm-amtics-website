/**
 * ACM AMTICS - Chapter Gallery Module Logic
 */

(function () {
    const STORAGE_KEY = 'acm_amtics_gallery_items';

    const defaultPhotos = [
        {
            id: 'g1',
            title: 'ACM AMTICS Campus & Innovation Hub',
            category: 'campus',
            categoryLabel: 'Campus Life',
            date: '10 Aug 2025',
            image: '/images/amtics-building.jpg',
            likes: 128,
            desc: 'The center of technological innovation, hackathons, and student collaboration at AMTICS.'
        },
        {
            id: 'g2',
            title: 'CodeStorm 2025 - 24H Chapter Hackathon',
            category: 'hackathon',
            categoryLabel: 'Hackathon',
            date: '10 Oct 2025',
            image: '/images/projects/amtics-navigator.jpg',
            likes: 215,
            desc: 'Over 150 student developers hacking through the night to build transformative solutions.'
        },
        {
            id: 'g3',
            title: 'UI/UX Design Mastery Workshop',
            category: 'workshop',
            categoryLabel: 'Workshop',
            date: '14 Dec 2025',
            image: '/images/projects/learnmate.jpg',
            likes: 94,
            desc: 'Hands-on wireframing, Figma components, and human-centric design patterns led by chapter heads.'
        },
        {
            id: 'g4',
            title: 'EcoTech Green Campus Pitch Showcase',
            category: 'talk',
            categoryLabel: 'Tech Talk',
            date: '15 Sep 2025',
            image: '/images/projects/ecotrack.jpg',
            likes: 83,
            desc: 'Student teams showcasing climate-tech and sustainable engineering innovations to industry judges.'
        },
        {
            id: 'g5',
            title: 'ACM AMTICS Executive Core Team 2025-26',
            category: 'campus',
            categoryLabel: 'Team',
            date: '28 Aug 2025',
            image: '/images/amtics-building.jpg',
            likes: 172,
            desc: 'Dedicated student leaders and faculty mentors driving our university student chapter forward.'
        },
        {
            id: 'g6',
            title: 'Intro to GenAI & Large Language Models',
            category: 'workshop',
            categoryLabel: 'Workshop',
            date: '25 Sep 2025',
            image: '/images/projects/learnmate.jpg',
            likes: 147,
            desc: 'Exploring prompt engineering, RAG pipelines, and building AI-assisted web platforms.'
        }
    ];

    let photos = [];
    let currentCategory = 'all';
    let searchQuery = '';
    let activeLightboxIndex = 0;

    function init() {
        const saved = localStorage.getItem(STORAGE_KEY);
        if (saved) {
            try {
                photos = JSON.parse(saved);
            } catch (e) {
                photos = [...defaultPhotos];
            }
        } else {
            photos = [...defaultPhotos];
            localStorage.setItem(STORAGE_KEY, JSON.stringify(photos));
        }

        renderGallery();
        updateCount();
    }

    function renderGallery() {
        const grid = document.getElementById('galleryPhotoGrid');
        if (!grid) return;

        let filtered = photos.filter(p => {
            const matchesCat = currentCategory === 'all' || p.category === currentCategory;
            const matchesQuery = !searchQuery || 
                p.title.toLowerCase().includes(searchQuery) || 
                p.categoryLabel.toLowerCase().includes(searchQuery) ||
                (p.desc && p.desc.toLowerCase().includes(searchQuery));
            return matchesCat && matchesQuery;
        });

        if (filtered.length === 0) {
            grid.innerHTML = `
                <div style="grid-column: 1 / -1; text-align: center; padding: 4rem 1rem; background: #FFFFFF; border-radius: 16px; border: 1px dashed #CBD5E1;">
                    <svg width="48" height="48" fill="none" viewBox="0 0 24 24" stroke="#94A3B8" style="margin: 0 auto 1rem auto; display: block;">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z" />
                    </svg>
                    <h3 style="font-size: 1.15rem; font-weight: 700; color: #1E293B;">No photos found</h3>
                    <p style="font-size: 0.875rem; color: #64748B; margin-top: 0.35rem;">Try selecting a different category or clearing your search term.</p>
                </div>
            `;
            return;
        }

        grid.innerHTML = filtered.map((photo, index) => `
            <div class="gallery-item-card" onclick="openLightbox(${index})">
                <div class="gallery-img-wrap">
                    <img src="${photo.image}" alt="${photo.title}" class="gallery-item-img" onerror="this.src='/images/amtics-building.jpg'" />
                    <div class="gallery-item-overlay">
                        <div class="gallery-overlay-top">
                            <span class="gallery-tag-pill">${photo.categoryLabel}</span>
                            <div class="gallery-zoom-icon">
                                <svg width="16" height="16" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0zM10 7v3m0 0v3m0-3h3m-3 0H7" />
                                </svg>
                            </div>
                        </div>
                        <div class="gallery-overlay-bottom">
                            <h4 class="gallery-overlay-title">${photo.title}</h4>
                            <div class="gallery-overlay-meta">
                                <span>${photo.date}</span>
                                <span>&bull;</span>
                                <span>${photo.likes} Likes</span>
                            </div>
                        </div>
                    </div>
                </div>
                <div class="gallery-card-body">
                    <div>
                        <div class="gallery-info-title">${photo.title}</div>
                        <div class="gallery-info-date">${photo.date}</div>
                    </div>
                    <button type="button" class="gallery-like-btn" onclick="event.stopPropagation(); toggleLike('${photo.id}')" title="Like photo">
                        <svg width="15" height="15" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4.318 6.318a4.5 4.5 0 000 6.364L12 20.364l7.682-7.682a4.5 4.5 0 00-6.364-6.364L12 7.636l-1.318-1.318a4.5 4.5 0 00-6.364 0z" />
                        </svg>
                        <span>${photo.likes}</span>
                    </button>
                </div>
            </div>
        `).join('');
    }

    function updateCount() {
        const countEl = document.getElementById('totalPhotosCount');
        if (countEl) countEl.innerText = photos.length;
    }

    // Window global handlers
    window.filterGalleryCategory = function (category, btn) {
        currentCategory = category;
        document.querySelectorAll('.gallery-cat-pill').forEach(b => b.classList.remove('active'));
        if (btn) btn.classList.add('active');
        renderGallery();
    };

    window.searchGalleryPhotos = function () {
        const input = document.getElementById('gallerySearchInput');
        searchQuery = (input ? input.value : '').trim().toLowerCase();
        renderGallery();
    };

    window.toggleLike = function (id) {
        const p = photos.find(x => x.id === id);
        if (p) {
            p.likes += 1;
            localStorage.setItem(STORAGE_KEY, JSON.stringify(photos));
            renderGallery();
        }
    };

    // Lightbox
    window.openLightbox = function (index) {
        activeLightboxIndex = index;
        const modal = document.getElementById('galleryLightboxModal');
        if (!modal) return;

        updateLightboxContent();
        modal.classList.add('active');
        modal.setAttribute('aria-hidden', 'false');
        document.body.style.overflow = 'hidden';
    };

    window.closeLightbox = function () {
        const modal = document.getElementById('galleryLightboxModal');
        if (modal) {
            modal.classList.remove('active');
            modal.setAttribute('aria-hidden', 'true');
            document.body.style.overflow = '';
        }
    };

    window.nextLightboxImage = function () {
        if (photos.length === 0) return;
        activeLightboxIndex = (activeLightboxIndex + 1) % photos.length;
        updateLightboxContent();
    };

    window.prevLightboxImage = function () {
        if (photos.length === 0) return;
        activeLightboxIndex = (activeLightboxIndex - 1 + photos.length) % photos.length;
        updateLightboxContent();
    };

    function updateLightboxContent() {
        const photo = photos[activeLightboxIndex];
        if (!photo) return;

        const img = document.getElementById('lightboxImg');
        const title = document.getElementById('lightboxTitle');
        const meta = document.getElementById('lightboxMeta');
        const likes = document.getElementById('lightboxLikes');
        const dlBtn = document.getElementById('lightboxDownloadBtn');

        if (img) img.src = photo.image;
        if (title) title.innerText = photo.title;
        if (meta) meta.innerText = `${photo.categoryLabel} • ${photo.date} • ${photo.desc || ''}`;
        if (likes) likes.innerText = photo.likes;
        if (dlBtn) dlBtn.href = photo.image;
    }

    window.toggleLightboxLike = function () {
        const photo = photos[activeLightboxIndex];
        if (photo) {
            photo.likes += 1;
            localStorage.setItem(STORAGE_KEY, JSON.stringify(photos));
            updateLightboxContent();
            renderGallery();
        }
    };

    // Upload Modal
    window.openUploadModal = function () {
        const modal = document.getElementById('galleryUploadModal');
        if (modal) {
            modal.classList.add('active');
            document.body.style.overflow = 'hidden';
            const dateInput = document.getElementById('photoDateInput');
            if (dateInput && !dateInput.value) {
                dateInput.value = new Date().toISOString().split('T')[0];
            }
        }
    };

    window.closeUploadModal = function () {
        const modal = document.getElementById('galleryUploadModal');
        if (modal) {
            modal.classList.remove('active');
            document.body.style.overflow = '';
        }
    };

    window.selectPresetImg = function (url) {
        const input = document.getElementById('photoUrlInput');
        if (input) input.value = url;
    };

    window.handleGalleryUpload = function (e) {
        e.preventDefault();
        const title = document.getElementById('photoTitleInput').value.trim();
        const category = document.getElementById('photoCategorySelect').value;
        const dateRaw = document.getElementById('photoDateInput').value;
        const url = document.getElementById('photoUrlInput').value.trim() || '/images/amtics-building.jpg';
        const desc = document.getElementById('photoDescInput').value.trim();

        if (!title) {
            alert('Please enter a photo title.');
            return;
        }

        const catMap = {
            hackathon: 'Hackathon',
            workshop: 'Workshop',
            talk: 'Tech Talk',
            campus: 'Campus Life'
        };

        const newPhoto = {
            id: 'g_' + Date.now(),
            title: title,
            category: category,
            categoryLabel: catMap[category] || 'Event',
            date: dateRaw || 'Recent',
            image: url,
            likes: 1,
            desc: desc
        };

        photos.unshift(newPhoto);
        localStorage.setItem(STORAGE_KEY, JSON.stringify(photos));
        closeUploadModal();
        renderGallery();
        updateCount();
        alert('Photo successfully added to Chapter Gallery! 🎉');
    };

    // Keyboard support
    document.addEventListener('keydown', function (e) {
        const modal = document.getElementById('galleryLightboxModal');
        if (modal && modal.classList.contains('active')) {
            if (e.key === 'Escape') closeLightbox();
            if (e.key === 'ArrowRight') nextLightboxImage();
            if (e.key === 'ArrowLeft') prevLightboxImage();
        }
    });

    // Initialize on DOMContentLoaded
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
