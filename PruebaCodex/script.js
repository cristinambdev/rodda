const dogs = [
  {name:'Milo',age:'2 years',ageGroup:'Adult',breed:'Golden Retriever mix',distance:'2 miles away',traits:['Sweet-natured','Loves walks','House-trained'],bio:'A gentle soul with a talent for making every day feel like the weekend. Milo loves long walks and an even longer cuddle.',shelter:'The Good Dog Project',img:'photo-1552053831-71594a27632d',likes:128},
  {name:'Olive',age:'8 months',ageGroup:'Puppy',breed:'Terrier mix',distance:'4 miles away',traits:['Playful','Good with dogs','Quick learner'],bio:'Equal parts curious and cuddly, Olive is ready to turn your home into her favorite place in the world.',shelter:'Brooklyn Animal Rescue',img:'photo-1548199973-03cce0bbc87b',likes:114},
  {name:'Benny',age:'4 years',ageGroup:'Adult',breed:'Beagle',distance:'6 miles away',traits:['Easygoing','Food motivated','Loves kids'],bio:'Benny is a champion lounger and a world-class sniffer. He is happiest with kind people and a sunny spot.',shelter:'Hearts & Paws NYC',img:'photo-1505628346881-b72b27e84530',likes:96},
  {name:'Clover',age:'5 months',ageGroup:'Puppy',breed:'Shepherd mix',distance:'3 miles away',traits:['Adventurous','Very cuddly','Training started'],bio:'This bright little explorer is learning all about the big world. Clover would love a family to grow up with.',shelter:'The Good Dog Project',img:'photo-1516734212186-a967f81ad0d7',likes:87},
  {name:'Frankie',age:'3 years',ageGroup:'Adult',breed:'Corgi mix',distance:'8 miles away',traits:['Big personality','City savvy','Loyal'],bio:'Small legs, huge heart. Frankie knows the best neighborhood routes and will always save you the sunny side of the sofa.',shelter:'Hearts & Paws NYC',img:'photo-1517849845537-4d257902454a',likes:73},
  {name:'Poppy',age:'1 year',ageGroup:'Adult',breed:'Labrador mix',distance:'5 miles away',traits:['Gentle','Great on leash','Loves fetch'],bio:'Poppy brings a little sunshine wherever she goes. She is affectionate, bright, and ready for her next chapter.',shelter:'Brooklyn Animal Rescue',img:'photo-1551717743-49959800b1f6',likes:62}
];

let index = 0;
let filter = 'All ages';
let view = 'discover';
let liked = JSON.parse(localStorage.getItem('goodpup-liked') || '[]');
const cardArea = document.querySelector('#card-area');
const toast = document.querySelector('#toast');
const photo = (dog, width = 800) => `https://images.unsplash.com/${dog.img}?auto=format&fit=crop&w=${width}&q=85`;

function filteredDogs() {
  return dogs.filter(dog => (filter === 'All ages' || dog.ageGroup === filter) && (view !== 'liked' || liked.includes(dog.name)));
}

function likeTotal(dog) {
  return dog.likes + (liked.includes(dog.name) ? 1 : 0);
}

function render() {
  const isTerms = view === 'terms';
  const isTopLiked = view === 'top-liked';
  document.querySelector('#discovery-view').hidden = isTerms || isTopLiked;
  document.querySelector('#top-liked-view').hidden = !isTopLiked;
  document.querySelector('#terms-view').hidden = !isTerms;
  document.querySelector('.site-footer').hidden = isTerms;
  if (isTerms) return;

  const list = filteredDogs();
  document.querySelectorAll('.nav button').forEach(button => button.classList.toggle('active', button.dataset.view === view));
  document.querySelector('#like-count').textContent = liked.length ? `(${liked.length})` : '';
  document.querySelector('#page-title').textContent = view === 'liked' ? 'Your heart’s picked these pups.' : 'Meet your new best friend.';
  document.querySelector('#page-subtitle').textContent = view === 'liked' ? 'The dogs you’ve saved are right here.' : 'Good things happen when you find your match.';

  const topLiked = [...dogs].sort((a, b) => likeTotal(b) - likeTotal(a)).slice(0, 4);
  document.querySelector('#top-liked-page-list').innerHTML = topLiked.map((dog, rank) => `
    <li><article class="top-liked-page-card"><span class="top-liked-page-rank">${String(rank + 1).padStart(2, '0')}</span>
      <img src="${photo(dog, 400)}" alt="${dog.name}, an adoptable ${dog.breed}" />
      <div class="top-liked-page-details"><span class="eyebrow">${dog.age} · ${dog.distance}</span><h2>${dog.name}</h2>
        <p>${dog.breed}</p><span class="top-liked-page-count">♥ ${likeTotal(dog)} likes</span></div>
      <button class="top-liked-discover" data-dog="${dog.name}">Meet ${dog.name} →</button>
    </article></li>`).join('');
  document.querySelectorAll('.top-liked-discover').forEach(button => button.onclick = () => {
    view = 'discover';
    filter = 'All ages';
    index = dogs.findIndex(dog => dog.name === button.dataset.dog);
    document.querySelectorAll('.filter').forEach(item => item.classList.toggle('active', item.dataset.filter === filter));
    render();
  });
  document.querySelector('#top-liked-list').innerHTML = topLiked.map((dog, rank) => `
    <li><button class="top-liked-item" data-dog="${dog.name}" aria-label="View ${dog.name}, ${likeTotal(dog)} likes">
      <span class="top-liked-rank">${rank + 1}</span><img src="${photo(dog, 100)}" alt="" />
      <span class="top-liked-details"><strong>${dog.name}</strong><small>${dog.breed}</small></span>
      <span class="top-liked-count">♥ ${likeTotal(dog)}</span>
    </button></li>`).join('');
  document.querySelectorAll('.top-liked-item').forEach(button => button.onclick = () => {
    view = 'discover';
    filter = 'All ages';
    index = dogs.findIndex(dog => dog.name === button.dataset.dog);
    document.querySelectorAll('.filter').forEach(item => item.classList.toggle('active', item.dataset.filter === filter));
    render();
  });

  if (!list.length) {
    cardArea.innerHTML = `<div class="dog-card empty"><div><div class="empty-icon">🐾</div><h2>${view === 'liked' ? 'Your likes are waiting' : 'No pups in this age group right now'}</h2><div>${view === 'liked' ? 'Tap the heart on a dog to save them here.' : 'Try another filter to meet more dogs.'}</div><button id="empty-action">${view === 'liked' ? 'Discover dogs' : 'Show all ages'}</button></div></div>`;
    document.querySelector('#empty-action').onclick = () => {
      if (view === 'liked') view = 'discover';
      filter = 'All ages';
      document.querySelectorAll('.filter').forEach(button => button.classList.toggle('active', button.dataset.filter === 'All ages'));
      render();
    };
    return;
  }

  index = ((index % list.length) + list.length) % list.length;
  const dog = list[index];
  cardArea.innerHTML = `<article class="dog-card"><div class="photo"><img src="${photo(dog)}" alt="${dog.name}, an adoptable ${dog.breed}"/><span class="badge">✦ READY FOR A HOME</span><span class="distance">⌖ ${dog.distance}</span><div class="photo-caption"><h2>${dog.name}, ${dog.age.split(' ')[0]}</h2><span>${dog.breed}</span></div></div><div class="card-info"><div class="traits">${dog.traits.map(trait => `<span class="trait">${trait}</span>`).join('')}</div><p class="dog-bio">${dog.bio}</p><div class="card-footer"><span>Listed by <span class="shelter-name">${dog.shelter}</span></span><span>♥ &nbsp;${likeTotal(dog)} people like ${dog.name}</span></div></div></article><div class="swipe-actions"><button class="swipe undo" title="Previous dog" aria-label="Previous dog">↶</button><button class="swipe skip" title="Pass" aria-label="Pass">×</button><button class="swipe like" title="Like" aria-label="Like">♥</button><button class="swipe super" title="Learn more" aria-label="Learn more">✦</button></div>`;
  cardArea.querySelector('.skip').onclick = () => advance();
  cardArea.querySelector('.like').onclick = () => likeDog(dog);
  cardArea.querySelector('.undo').onclick = () => { index--; render(); };
  cardArea.querySelector('.super').onclick = () => showToast(`${dog.name} is waiting to meet you!`);
  document.querySelector('#progress-count').textContent = `${Math.min(index + 1, list.length)} / ${list.length}`;

  const chosen = dogs.filter(dogItem => liked.includes(dogItem.name)).slice(0, 3);
  document.querySelector('#mini-avatars').innerHTML = chosen.length
    ? chosen.map(dogItem => `<img class="mini-dog" src="${photo(dogItem, 100)}" alt="${dogItem.name}"/>`).join('') + `<span class="more">+${Math.max(0, liked.length - 3)}</span>`
    : '<span class="mini-dog" style="display:grid;place-items:center;background:#e1d5c8;font-size:18px">🐶</span><span class="mini-dog" style="display:grid;place-items:center;background:#d8dfd4;font-size:18px">🐕</span><span class="mini-dog" style="display:grid;place-items:center;background:#e6dded;font-size:18px">🐾</span>';
}

function saveLikes() {
  localStorage.setItem('goodpup-liked', JSON.stringify(liked));
}

function advance() {
  index = (index + 1) % Math.max(1, filteredDogs().length);
  render();
  showToast('On to the next pup');
}

function likeDog(dog) {
  if (!liked.includes(dog.name)) {
    liked.push(dog.name);
    saveLikes();
  }
  showToast(`${dog.name} saved to Your likes ♥`);
  advance();
}

let toastTimer;
function showToast(message) {
  toast.textContent = message;
  toast.classList.add('show');
  clearTimeout(toastTimer);
  toastTimer = setTimeout(() => toast.classList.remove('show'), 2200);
}

document.querySelectorAll('.filter').forEach(button => button.onclick = () => {
  filter = button.dataset.filter;
  index = 0;
  document.querySelectorAll('.filter').forEach(item => item.classList.toggle('active', item === button));
  render();
});

document.querySelectorAll('.nav button').forEach(button => button.onclick = () => {
  if (['messages', 'profile'].includes(button.dataset.view)) {
    showToast('Coming soon — we’re making room for more good things!');
    return;
  }
  view = button.dataset.view;
  index = 0;
  render();
});

document.querySelector('#see-likes').onclick = event => {
  event.preventDefault();
  view = 'liked';
  render();
};
document.querySelector('#browse-more').onclick = () => { view = 'discover'; render(); };
document.querySelector('#shelter-btn').onclick = () => showToast('Shelter onboarding is coming soon!');
document.querySelector('#terms-link').onclick = event => {
  event.preventDefault();
  view = 'terms';
  render();
  window.scrollTo({top: 0, behavior: 'smooth'});
};
document.querySelector('#terms-back').onclick = () => {
  view = 'discover';
  index = 0;
  render();
};

render();
