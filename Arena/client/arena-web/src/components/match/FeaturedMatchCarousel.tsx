import { FC } from 'react';
import { Settings, Maximize2, Volume2, Users, ChevronLeft, ChevronRight } from 'lucide-react';

export const FeaturedMatchCarousel: FC = () => {
  return (
    <>
      <div className="player-chat-row">
        <div className="player-pane">
          <div className="live-tag-wrap">
            <div className="live-tag"><span className="pulse-dot"></span>LIVE</div>
            <div className="viewer-tag">
              <Users size={12} />
              12.4K
            </div>
          </div>

          <div className="video-controls">
            <div className="left">
              <button aria-label="Mute">
                <Volume2 size={16} />
              </button>
              <span className="quality-tag">1080P</span>
            </div>
            <div className="right">
              <button aria-label="Settings">
                <Settings size={16} />
              </button>
              <button aria-label="Fullscreen">
                <Maximize2 size={16} />
              </button>
            </div>
          </div>
        </div>

        <aside className="chat-pane">
          <div className="chat-header">
            <Users size={16} />
            Chat
          </div>
          <div className="chat-messages">
            <div className="msg"><span className="user">Domazing:</span> seems unnecessary</div>
            <div className="msg"><span className="badge-tag">MOD</span><span className="user mod">Kessler_ow:</span> keep it civil in here</div>
            <div className="msg"><span className="user">isuckatpking:</span> hollow sigil throwing so hard rn 💀</div>
            <div className="msg"><span className="badge-tag">SUB</span><span className="user sub">Baldruckr_ziggys:</span> LETS GOOO</div>
            <div className="msg"><span className="user">Venusian:</span> can we get a replay of that clutch</div>
            <div className="msg"><span className="user">isuckatpking:</span> gg either way</div>
            <div className="msg"><span className="badge-tag">SUB</span><span className="user sub">Baldruckr_ziggys:</span> ez</div>
            <div className="msg"><span className="user">Kinganon253:</span> dh bomber!!</div>
            <div className="msg"><span className="user">Venusian:</span> who's casting this one</div>
          </div>
          <div className="chat-input">
            <input type="text" placeholder="Send a message" />
          </div>
        </aside>
      </div>

      <div className="channel-bar">
        <div className="channel-avatar">HV</div>
        <div className="channel-info">
          <h3>HollowVOD</h3>
          <div className="title">GRAND FINALS DAY 2 | Hollow Sigil vs Ninefold — !bracket !discord !merch</div>
          <div className="channel-tags">
            <span>Tactical Shooter</span>
            <span>BO5</span>
            <span>English</span>
          </div>
        </div>
        <button className="watch-now">Watch now</button>
        <div className="channel-dots">
          <span className="active"></span><span></span><span></span><span></span><span></span>
        </div>
        <div className="channel-nav">
          <button aria-label="Previous channel">
            <ChevronLeft size={18} />
          </button>
          <button aria-label="Next channel">
            <ChevronRight size={18} />
          </button>
        </div>
      </div>

      <section className="live-event-banner">
        <span className="event-badge">LIVE</span>
        <button className="event-cta">Go to event</button>
        <div className="event-content">
          <h3>Arena Championship LAN</h3>
          <p>Eight teams, single elimination, one stage. Follow the bracket live as regional qualifiers battle for the championship slot and a share of the prize pool.</p>
        </div>
      </section>
    </>
  );
};

export default FeaturedMatchCarousel;
