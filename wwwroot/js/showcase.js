document.addEventListener('DOMContentLoaded', () => {
  const cards = [...document.querySelectorAll('.project-card')];
  const updateProjects = () => {
    const query = (document.getElementById('projectSearch')?.value || '').toLowerCase();
    const category = document.getElementById('categoryFilter')?.value || '';
    const tech = document.getElementById('techFilter')?.value || '';
    const year = document.getElementById('yearFilter')?.value || '';
    cards.forEach(card => card.hidden = !(card.dataset.name.toLowerCase().includes(query) || card.dataset.tech.toLowerCase().includes(query) || card.textContent.toLowerCase().includes(query)) || (category && card.dataset.cat !== category) || (tech && !card.dataset.tech.includes(tech)) || (year && card.dataset.year !== year));
    if (document.getElementById('projectSort')?.value === 'az') [...cards].sort((a,b) => a.dataset.name.localeCompare(b.dataset.name)).forEach(card => card.parentElement.appendChild(card));
    else [...cards].sort((a,b) => b.dataset.year.localeCompare(a.dataset.year)).forEach(card => card.parentElement.appendChild(card));
  };
  ['projectSearch','categoryFilter','techFilter','yearFilter','projectSort'].forEach(id => document.getElementById(id)?.addEventListener(id === 'projectSearch' ? 'input' : 'change', updateProjects));
  let selected = 'All';
  document.querySelectorAll('#galleryCategories button').forEach(button => button.addEventListener('click', () => {
    selected = button.dataset.category;
    document.querySelectorAll('#galleryCategories button').forEach(item => item.classList.toggle('selected', item === button));
    updateGallery();
  }));
  function updateGallery() {
    const query = (document.getElementById('gallerySearch')?.value || '').toLowerCase();
    const galleryCards = [...document.querySelectorAll('.gallery-card')];
    galleryCards.forEach(card => card.hidden = (selected !== 'All' && card.dataset.category !== selected) || !card.dataset.title.toLowerCase().includes(query));
    const sortOrder = document.getElementById('gallerySort')?.value;
    galleryCards.sort((a, b) => sortOrder === 'Oldest First'
      ? a.dataset.date.localeCompare(b.dataset.date)
      : b.dataset.date.localeCompare(a.dataset.date))
      .forEach(card => card.parentElement.appendChild(card));
  }
  document.getElementById('gallerySearch')?.addEventListener('input', updateGallery);
  document.getElementById('gallerySort')?.addEventListener('change', updateGallery);
});
