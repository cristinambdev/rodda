import { getTopBirds } from '../data/birds';

export default function Sidebar({ isOpen, onToggle }) {
  const topBirds = getTopBirds(5);

  return (
    <>
      <button
        className="sidebar-toggle"
        onClick={onToggle}
        aria-label={isOpen ? 'Close leaderboard' : 'Open leaderboard'}
      >
        <span className="toggle-icon">🏆</span>
        <span className="toggle-text">Top Birds</span>
      </button>

      <aside className={`sidebar ${isOpen ? 'open' : ''}`} role="complementary">
        <div className="sidebar-header">
          <h2>🏆 Most Liked Birdies</h2>
          <button
            className="sidebar-close"
            onClick={onToggle}
            aria-label="Close sidebar"
          >
            ✕
          </button>
        </div>

        <div className="sidebar-content">
          <ol className="bird-ranking">
            {topBirds.map((bird, index) => (
              <li key={bird.id} className="rank-item">
                <span className="rank-number">{index + 1}</span>
                <span className="rank-emoji">{bird.emoji}</span>
                <div className="rank-info">
                  <span className="rank-name">{bird.name}</span>
                  <span className="rank-species">{bird.species}</span>
                </div>
                <span className="rank-likes">
                  <span className="heart">💚</span>
                  {bird.likes.toLocaleString()}
                </span>
              </li>
            ))}
          </ol>

          <p className="sidebar-footer">* Mock data for demo purposes</p>
        </div>
      </aside>

      {isOpen && (
        <div className="sidebar-overlay" onClick={onToggle} aria-hidden="true" />
      )}
    </>
  );
}