document.addEventListener('DOMContentLoaded', () => {
  /* ==========================================================================
     Project Showcase Filtering & Sorting
     ========================================================================== */
  const cards = [...document.querySelectorAll('.project-card')];
  
  const updateProjects = () => {
    const query = (document.getElementById('projectSearch')?.value || '').trim().toLowerCase();
    const category = document.getElementById('categoryFilter')?.value || '';
    const tech = document.getElementById('techFilter')?.value || '';
    const year = document.getElementById('yearFilter')?.value || '';

    cards.forEach(card => {
      const name = (card.dataset.name || '').toLowerCase();
      const cardTech = (card.dataset.tech || '').toLowerCase();
      const cardCat = card.dataset.cat || '';
      const cardYear = card.dataset.year || '';
      const text = card.textContent.toLowerCase();

      const matchesSearch = !query || name.includes(query) || cardTech.includes(query) || text.includes(query);
      const matchesCat = !category || cardCat === category;
      const matchesTech = !tech || cardTech.includes(tech.toLowerCase());
      const matchesYear = !year || cardYear === year;

      card.style.display = (matchesSearch && matchesCat && matchesTech && matchesYear) ? '' : 'none';
    });

    const sortVal = document.getElementById('projectSort')?.value;
    const visibleCards = cards.filter(c => c.style.display !== 'none');
    const parent = cards[0]?.parentElement;

    if (parent) {
      if (sortVal === 'az') {
        visibleCards.sort((a, b) => (a.dataset.name || '').localeCompare(b.dataset.name || ''));
      } else {
        visibleCards.sort((a, b) => (b.dataset.year || '').localeCompare(a.dataset.year || ''));
      }
      visibleCards.forEach(card => parent.appendChild(card));
    }
  };

  ['projectSearch', 'categoryFilter', 'techFilter', 'yearFilter', 'projectSort'].forEach(id => {
    const el = document.getElementById(id);
    if (el) {
      el.addEventListener(id === 'projectSearch' ? 'input' : 'change', updateProjects);
    }
  });

  /* ==========================================================================
     Gallery Showcase Filtering, Sorting, Lightbox & Mobile Interaction
     ========================================================================== */
  const galleryGrid = document.getElementById('galleryGrid');
  const gallerySearch = document.getElementById('gallerySearch');
  const gallerySort = document.getElementById('gallerySort');
  const galleryCounter = document.getElementById('galleryCounter');
  const galleryClearBtn = document.getElementById('galleryClearBtn');
  const galleryEmptyState = document.getElementById('galleryEmptyState');
  const galleryResetEmptyBtn = document.getElementById('galleryResetEmptyBtn');
  const categoryButtons = document.querySelectorAll('#galleryCategories button');
  const galleryCards = [...document.querySelectorAll('.gallery-card')];

  let selectedCategory = 'All';

  categoryButtons.forEach(button => {
    button.addEventListener('click', () => {
      selectedCategory = button.dataset.category || 'All';
      categoryButtons.forEach(item => {
        const isSelected = item === button;
        item.classList.toggle('selected', isSelected);
        item.setAttribute('aria-selected', isSelected ? 'true' : 'false');
      });
      updateGallery();
    });
  });

  function updateGallery() {
    const query = (gallerySearch?.value || '').trim().toLowerCase();
    let visibleCount = 0;

    galleryCards.forEach(card => {
      const cardCat = card.dataset.category || '';
      const cardTitle = (card.dataset.title || '').toLowerCase();
      const cardText = card.textContent.toLowerCase();

      // Normalize match logic: "Talks & Seminars" matches both "Talks & Seminars" and "Talks"
      const matchesCat = selectedCategory === 'All' || 
                         cardCat === selectedCategory || 
                         (selectedCategory === 'Talks & Seminars' && cardCat === 'Talks') ||
                         (selectedCategory === 'Talks' && cardCat === 'Talks & Seminars');
                         
      const matchesSearch = !query || cardTitle.includes(query) || cardText.includes(query);

      const isVisible = matchesCat && matchesSearch;
      card.style.display = isVisible ? '' : 'none';
      if (isVisible) visibleCount++;
    });

    // Update Counter & Reset Button
    if (galleryCounter) {
      if (selectedCategory === 'All' && !query) {
        galleryCounter.textContent = `Showing all ${visibleCount} albums`;
        if (galleryClearBtn) galleryClearBtn.style.display = 'none';
      } else {
        const catLabel = selectedCategory === 'All' ? 'All categories' : selectedCategory;
        galleryCounter.textContent = `Showing ${visibleCount} of ${galleryCards.length} albums (${catLabel})`;
        if (galleryClearBtn) galleryClearBtn.style.display = 'inline-block';
      }
    }

    // Toggle Empty State
    if (galleryEmptyState) {
      galleryEmptyState.style.display = visibleCount === 0 ? 'block' : 'none';
    }

    // Sort visible cards
    const sortOrder = gallerySort?.value || 'newest';
    const visibleCards = galleryCards.filter(c => c.style.display !== 'none');

    if (galleryGrid) {
      visibleCards.sort((a, b) => {
        const dateA = a.dataset.date || '';
        const dateB = b.dataset.date || '';
        return (sortOrder === 'oldest' || sortOrder === 'Oldest First') 
          ? dateA.localeCompare(dateB) 
          : dateB.localeCompare(dateA);
      }).forEach(card => galleryGrid.appendChild(card));
    }
  }

  function resetGalleryFilters() {
    selectedCategory = 'All';
    if (gallerySearch) gallerySearch.value = '';
    categoryButtons.forEach(btn => {
      const isAll = (btn.dataset.category || '') === 'All';
      btn.classList.toggle('selected', isAll);
      btn.setAttribute('aria-selected', isAll ? 'true' : 'false');
    });
    updateGallery();
  }

  if (gallerySearch) gallerySearch.addEventListener('input', updateGallery);
  if (gallerySort) gallerySort.addEventListener('change', updateGallery);
  if (galleryClearBtn) galleryClearBtn.addEventListener('click', resetGalleryFilters);
  if (galleryResetEmptyBtn) galleryResetEmptyBtn.addEventListener('click', resetGalleryFilters);

  // Initial update
  if (galleryCards.length > 0) {
    updateGallery();
  }

  /* ==========================================================================
     Lightbox Modal Preview Implementation
     ========================================================================== */
  const lightbox = document.getElementById('galleryLightbox');
  const lightboxBackdrop = document.getElementById('lightboxBackdrop');
  const lightboxClose = document.getElementById('lightboxClose');
  const lightboxImg = document.getElementById('lightboxImg');
  const lightboxTag = document.getElementById('lightboxTag');
  const lightboxTitle = document.getElementById('lightboxTitle');
  const lightboxDate = document.getElementById('lightboxDate');

  function openLightbox(card) {
    if (!lightbox) return;
    const img = card.querySelector('.gallery-photo img');
    const title = card.querySelector('.gallery-title')?.textContent || '';
    const tag = card.querySelector('.tag')?.textContent || '';
    const date = card.querySelector('.date-chip')?.textContent?.trim() || '';

    if (lightboxImg && img) {
      lightboxImg.src = img.src;
      lightboxImg.alt = title;
    }
    if (lightboxTitle) lightboxTitle.textContent = title;
    if (lightboxTag) lightboxTag.textContent = tag;
    if (lightboxDate) lightboxDate.textContent = date;

    lightbox.style.display = 'flex';
    document.body.style.overflow = 'hidden';
  }

  function closeLightbox() {
    if (!lightbox) return;
    lightbox.style.display = 'none';
    document.body.style.overflow = '';
  }

  galleryCards.forEach(card => {
    card.addEventListener('click', (e) => {
      openLightbox(card);
    });
    card.addEventListener('keydown', (e) => {
      if (e.key === 'Enter' || e.key === ' ') {
        e.preventDefault();
        openLightbox(card);
      }
    });
  });

  if (lightboxClose) lightboxClose.addEventListener('click', closeLightbox);
  if (lightboxBackdrop) lightboxBackdrop.addEventListener('click', closeLightbox);

  document.addEventListener('keydown', (e) => {
    if (e.key === 'Escape' && lightbox && lightbox.style.display !== 'none') {
      closeLightbox();
    }
  });
});
