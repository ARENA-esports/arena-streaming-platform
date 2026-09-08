import React from 'react';
import { AlertCircle } from 'lucide-react';

interface FallbackAlertProps {
  message?: string;
}

export const FallbackAlert: React.FC<FallbackAlertProps> = ({ 
  message = "Broadcast has not yet been linked by the Streamer." 
}) => {
  return (
    <div className="w-full aspect-video bg-arena-surface border border-arena-border border-dashed rounded-sm flex flex-col items-center justify-center p-6 text-center">
      <div className="w-16 h-16 rounded-full bg-arena-bg border border-arena-border flex items-center justify-center mb-4 text-arena-textMuted">
        <AlertCircle size={32} />
      </div>
      <h3 className="text-xl font-display font-bold text-white tracking-widest uppercase mb-2">
        Stream Offline
      </h3>
      <p className="text-arena-textMuted font-sans max-w-md">
        {message}
      </p>
    </div>
  );
};

export default FallbackAlert;
