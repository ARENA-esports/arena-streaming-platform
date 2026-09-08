import { FC, ReactNode, useState } from 'react';
import TopBar from './TopBar';
import Sidebar from './Sidebar';

interface DashboardLayoutProps {
  children: ReactNode;
}

export const DashboardLayout: FC<DashboardLayoutProps> = ({ children }) => {
  const [isSidebarExpanded, setIsSidebarExpanded] = useState(false);

  const toggleSidebar = () => {
    setIsSidebarExpanded(prev => !prev);
  };

  return (
    <div className="flex h-screen overflow-hidden">
      <Sidebar isExpanded={isSidebarExpanded} toggleSidebar={toggleSidebar} />
      <div className="flex-1 flex flex-col min-w-0 h-screen overflow-y-auto">
        <TopBar />
        <main className="stream-main">
          {children}
        </main>
      </div>
    </div>
  );
};

export default DashboardLayout;
