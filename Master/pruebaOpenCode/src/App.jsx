import { useState, useCallback } from 'react';
import { birds } from './data/birds';
import SwipeableCard from './components/SwipeableCard';
import Sidebar from './components/Sidebar';
import './App.css';

function App() {
  const [currentIndex, setCurrentIndex] = useState(0);
  const [likes, setLikes] = useState(0);
  const [passes, setPasses] = useState(0);
  const [showRestart, setShowRestart] = useState(false);
  const [sidebarOpen, setSidebarOpen] = useState(false);

  const currentBird = birds[currentIndex];
  const isLast = currentIndex === birds.length - 1;

  const handleSwipeRight = useCallback((bird) => {
    setLikes((l) => l + 1);
    if (isLast) {
      setShowRestart(true);
    } else {
      setCurrentIndex((i) => i + 1);
    }
  }, [isLast]);

  const handleSwipeLeft = useCallback((bird) => {
    setPasses((p) => p + 1);
    if (isLast) {
      setShowRestart(true);
    } else {
      setCurrentIndex((i) => i + 1);
    }
  }, [isLast]);

  const handleRestart = () => {
    setCurrentIndex(0);
    setLikes(0);
    setPasses(0);
    setShowRestart(false);
  };

  const toggleSidebar = () => setSidebarOpen((open) => !open);

  if (showRestart) {
    return (
      <div className="app">
        <Sidebar isOpen={sidebarOpen} onToggle={toggleSidebar} />
        <header className="header">
          <h1>🐦 Bird Tinder</h1>
          <p className="subtitle">Find your feathered match!</p>
        </header>

        <div className="match-screen">
          <div className="celebration">🎉🐦🎉</div>
          <h2>You've seen all the birdies!</h2>
          <div className="final-stats">
            <div className="stat">
              <span className="stat-number">{likes}</span>
              <span className="stat-label">💚 Likes</span>
            </div>
            <div className="stat">
              <span className="stat-number">{passes}</span>
              <span className="stat-label">💔 Passes</span>
            </div>
          </div>
          <button className="restart-btn" onClick={handleRestart}>
            Meet more birds! →
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="app">
      <Sidebar isOpen={sidebarOpen} onToggle={toggleSidebar} />

      <header className="header">
        <h1>🐦 Bird Tinder</h1>
        <p className="subtitle">Swipe to find your feathered friend</p>
      </header>

      <div className="stats-bar">
        <div className="stat-mini">
          <span className="stat-heart">💚</span>
          <span>{likes}</span>
        </div>
        <div className="stat-mini">
          <span className="stat-heart">💔</span>
          <span>{passes}</span>
        </div>
        <div className="stat-mini">
          <span className="stat-heart">🐦</span>
          <span>{currentIndex + 1}/{birds.length}</span>
        </div>
      </div>

      <main className="main">
        {currentBird && (
          <SwipeableCard
            bird={currentBird}
            onSwipeLeft={handleSwipeLeft}
            onSwipeRight={handleSwipeRight}
            isLast={isLast}
          />
        )}

        <div className="hint">
          ← Drag left to pass · Drag right to like →
        </div>
      </main>

      <footer className="footer">
        <p>Made with 💚 for bird lovers</p>
      </footer>
    </div>
  );
}

export default App;