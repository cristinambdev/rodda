import { useState, useRef, useEffect } from 'react';

const SWIPE_THRESHOLD = 100;
const MAX_ROTATION = 20;

export default function SwipeableCard({ bird, onSwipeLeft, onSwipeRight, isLast }) {
  const [offset, setOffset] = useState({ x: 0, y: 0 });
  const [isDragging, setIsDragging] = useState(false);
  const startRef = useRef({ x: 0, y: 0 });
  const cardRef = useRef(null);

  const handleStart = (e) => {
    const clientX = e.touches ? e.touches[0].clientX : e.clientX;
    const clientY = e.touches ? e.touches[0].clientY : e.clientY;
    startRef.current = { x: clientX, y: clientY };
    setIsDragging(true);
    cardRef.current.style.transition = 'none';
  };

  const handleMove = (e) => {
    if (!isDragging) return;
    e.preventDefault();

    const clientX = e.touches ? e.touches[0].clientX : e.clientX;
    const clientY = e.touches ? e.touches[0].clientY : e.clientY;

    const deltaX = clientX - startRef.current.x;
    const deltaY = clientY - startRef.current.y;

    setOffset({ x: deltaX, y: deltaY });
  };

  const handleEnd = () => {
    if (!isDragging) return;
    setIsDragging(false);
    cardRef.current.style.transition = 'transform 0.3s cubic-bezier(0.34, 1.56, 0.64, 1)';

    const { x } = offset;

    if (x > SWIPE_THRESHOLD) {
      cardRef.current.style.transform = `translateX(500px) rotate(${MAX_ROTATION}deg)`;
      setTimeout(() => onSwipeRight(bird), 200);
    } else if (x < -SWIPE_THRESHOLD) {
      cardRef.current.style.transform = `translateX(-500px) rotate(${-MAX_ROTATION}deg)`;
      setTimeout(() => onSwipeLeft(bird), 200);
    } else {
      setOffset({ x: 0, y: 0 });
    }
  };

  useEffect(() => {
    window.addEventListener('mousemove', handleMove);
    window.addEventListener('mouseup', handleEnd);
    window.addEventListener('touchmove', handleMove, { passive: false });
    window.addEventListener('touchend', handleEnd);
    return () => {
      window.removeEventListener('mousemove', handleMove);
      window.removeEventListener('mouseup', handleEnd);
      window.removeEventListener('touchmove', handleMove);
      window.removeEventListener('touchend', handleEnd);
    };
  }, [isDragging, offset]);

  const rotation = (offset.x / window.innerWidth) * MAX_ROTATION;

  const getIndicatorStyle = (direction) => {
    const opacity = Math.min(Math.abs(offset.x) / SWIPE_THRESHOLD, 1);
    if (direction === 'left' && offset.x < 0) return { opacity, color: '#ff6b6b' };
    if (direction === 'right' && offset.x > 0) return { opacity, color: '#6bff6b' };
    return { opacity: 0 };
  };

  return (
    <div
      ref={cardRef}
      className="bird-card"
      style={{
        transform: `translate(${offset.x}px, ${offset.y}px) rotate(${rotation}deg)`,
        cursor: isDragging ? 'grabbing' : 'grab',
      }}
      onMouseDown={handleStart}
      onTouchStart={handleStart}
    >
      <div className="card-inner">
        <div className="bird-emoji">{bird.emoji}</div>
        <div className="bird-info">
          <h2 className="bird-name">
            {bird.name}
            <span className="bird-species">{bird.species}</span>
          </h2>
          <p className="bird-bio">{bird.bio}</p>
          <div className="bird-details">
            <span className="fun-fact">✨ {bird.funFact}</span>
            <span className="habitat">🏠 {bird.habitat}</span>
          </div>
        </div>
      </div>

      <div
        className={`swipe-indicator swipe-left ${offset.x < -50 ? 'visible' : ''}`}
        style={getIndicatorStyle('left')}
      >
        <span>NOPE 💔</span>
      </div>
      <div
        className={`swipe-indicator swipe-right ${offset.x > 50 ? 'visible' : ''}`}
        style={getIndicatorStyle('right')}
      >
        <span>LIKE 💚</span>
      </div>

      {isLast && <div className="last-card-hint">Last birdie! 🐦</div>}
    </div>
  );
}