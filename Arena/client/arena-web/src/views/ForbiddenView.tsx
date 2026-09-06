import React from 'react';
import { useNavigate } from 'react-router-dom';
import { ShieldAlert } from 'lucide-react';
import Button from '../components/common/Button';

export const ForbiddenView: React.FC = () => {
  const navigate = useNavigate();

  return (
    <div className="min-h-[calc(100vh-4rem)] flex items-center justify-center bg-arena-bg py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-md w-full bg-arena-surface p-10 rounded-sm border border-arena-crimson border-dashed text-center">
        <div className="flex justify-center mb-6">
          <div className="w-20 h-20 rounded-full bg-arena-crimson/10 border border-arena-crimson flex items-center justify-center text-arena-crimson">
            <ShieldAlert size={40} />
          </div>
        </div>
        <h2 className="text-3xl font-display font-black text-white tracking-widest uppercase mb-4">
          Access Denied
        </h2>
        <p className="text-sm text-arena-textMuted font-sans mb-8">
          Your current role does not have the required permissions to access this sector of the Arena.
        </p>
        <Button variant="primary" onClick={() => navigate('/')}>
          Return to Dashboard
        </Button>
      </div>
    </div>
  );
};

export default ForbiddenView;
