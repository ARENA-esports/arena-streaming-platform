import React, { useState } from 'react';
import { MatchResponse, StreamResponse } from '../../types';

interface MatchSidePanelProps {
  match: MatchResponse;
  stream: StreamResponse | null;
}

export const MatchSidePanel: React.FC<MatchSidePanelProps> = ({ match, stream }) => {
  const [activeTab, setActiveTab] = useState<'details'>('details');

  return (
    <div className="bg-arena-surface border border-arena-border rounded-[14px] flex flex-col h-full overflow-hidden">
      {/* Tabs Header */}
      <div className="flex border-b border-arena-border bg-[#161B22]">
        <button
          className={`flex-1 py-4 text-sm font-semibold transition-colors ${
            activeTab === 'details' 
              ? 'text-white border-b-2 border-arena-cyan' 
              : 'text-arena-textMuted hover:text-white'
          }`}
          onClick={() => setActiveTab('details')}
        >
          Details
        </button>
      </div>

      {/* Tab Content area */}
      <div className="p-6 flex-grow overflow-y-auto">
        {activeTab === 'details' && (
          <div className="space-y-8">
            
            {/* Teams Section */}
            <div>
              <h3 className="text-lg font-semibold text-white mb-4 border-b border-arena-border pb-2">
                Teams
              </h3>
              <div className="flex justify-center items-center space-x-6 py-4">
                <div className="flex flex-col items-center">
                  <div className="w-16 h-16 rounded-full bg-arena-bg border border-arena-border flex items-center justify-center shadow-inner mb-2">
                    <span className="text-xl font-bold text-white">T{match.teamAId}</span>
                  </div>
                </div>
                <div className="text-arena-textMuted font-bold text-xl italic">VS</div>
                <div className="flex flex-col items-center">
                  <div className="w-16 h-16 rounded-full bg-arena-bg border border-arena-border flex items-center justify-center shadow-inner mb-2">
                    <span className="text-xl font-bold text-white">T{match.teamBId}</span>
                  </div>
                </div>
              </div>
            </div>

            {/* Match Status & Stream Details */}
            <div>
              <h3 className="text-lg font-semibold text-white mb-4 border-b border-arena-border pb-2">
                Stream Info
              </h3>
              <div className="space-y-4">
                <div>
                  <p className="text-xs text-arena-textMuted font-semibold mb-1">Status</p>
                  <p className="font-semibold text-white">{match.status}</p>
                </div>
                {stream && (
                  <>
                    <div>
                      <p className="text-xs text-arena-textMuted font-semibold mb-1">Broadcaster</p>
                      <p className="font-semibold text-arena-cyan">{stream.channelName}</p>
                    </div>
                    {stream.startedAt && (
                      <div>
                        <p className="text-xs text-arena-textMuted font-semibold mb-1">Started At</p>
                        <p className="font-mono text-sm text-white">{new Date(stream.startedAt).toLocaleString()}</p>
                      </div>
                    )}
                  </>
                )}
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

export default MatchSidePanel;
