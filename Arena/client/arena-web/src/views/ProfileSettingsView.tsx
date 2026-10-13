import React, { useState, useEffect } from 'react';
import { useAuth } from '../context/AuthContext';
import { userService, UpdateProfileRequest } from '../api/userService';
import { authService } from '../api/authService';
import { Camera, Upload, Image as ImageIcon } from 'lucide-react';

export const ProfileSettingsView: React.FC = () => {
  const { user, refreshProfile, logout } = useAuth();
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  const [formData, setFormData] = useState<UpdateProfileRequest>({
    username: '',
    displayName: '',
    bio: '',
    avatarUrl: '',
    bannerUrl: ''
  });

  // Password Modal State
  const [isPasswordModalOpen, setIsPasswordModalOpen] = useState(false);
  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [pwdError, setPwdError] = useState('');
  const [pwdSuccess, setPwdSuccess] = useState('');
  const [isChangingPwd, setIsChangingPwd] = useState(false);

  // Delete Account Modal State
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
  const [confirmUsername, setConfirmUsername] = useState('');
  const [deleteError, setDeleteError] = useState('');
  const [isDeleting, setIsDeleting] = useState(false);

  useEffect(() => {
    const fetchProfile = async () => {
      try {
        const data = await userService.getProfile();
        setFormData({
          username: data.username,
          displayName: data.displayName || data.username,
          bio: data.bio || '',
          avatarUrl: data.avatarUrl || '',
          bannerUrl: data.bannerUrl || ''
        });
      } catch (err) {
        setError('Failed to load profile settings');
      } finally {
        setLoading(false);
      }
    };
    fetchProfile();
  }, []);

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
    setFormData({
      ...formData,
      [e.target.name]: e.target.value
    });
  };

  const saveMedia = async (avatar?: string, banner?: string) => {
    setSaving(true);
    setError('');
    setSuccess('');
    try {
      const payload: UpdateProfileRequest = {
        ...formData,
        avatarUrl: avatar !== undefined ? avatar : (formData.avatarUrl || undefined),
        bannerUrl: banner !== undefined ? banner : (formData.bannerUrl || undefined)
      };
      await userService.updateProfile(payload);
      await refreshProfile();
      setSuccess('Profile branding & media saved successfully!');
    } catch (err: any) {
      setError(err.response?.data?.message || err.message || 'Failed to save profile media');
    } finally {
      setSaving(false);
    }
  };

  const handleAvatarFileUpload = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    const reader = new FileReader();
    reader.onload = () => {
      if (typeof reader.result === 'string') {
        const val = reader.result;
        setFormData(prev => ({ ...prev, avatarUrl: val }));
        saveMedia(val, undefined);
      }
    };
    reader.readAsDataURL(file);
  };

  const handleBannerFileUpload = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    const reader = new FileReader();
    reader.onload = () => {
      if (typeof reader.result === 'string') {
        const val = reader.result;
        setFormData(prev => ({ ...prev, bannerUrl: val }));
        saveMedia(undefined, val);
      }
    };
    reader.readAsDataURL(file);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    setError('');
    setSuccess('');
    try {
      const payload = {
        ...formData,
        avatarUrl: formData.avatarUrl || undefined,
        bannerUrl: formData.bannerUrl || undefined
      };
      await userService.updateProfile(payload);
      await refreshProfile();
      setSuccess('Profile updated successfully.');
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to update profile');
    } finally {
      setSaving(false);
    }
  };

  const handleChangeRole = async () => {
    const newRole = user?.role === 'Viewer' ? 'Organizer' : 'Viewer';
    setSaving(true);
    setError('');
    setSuccess('');
    try {
      await userService.changeRole(newRole);
      await authService.refresh();
      await refreshProfile();
      setSuccess(`Successfully changed role to ${newRole}.`);
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to change role');
    } finally {
      setSaving(false);
    }
  };

  const handleChangePassword = async () => {
    setIsChangingPwd(true);
    setPwdError('');
    setPwdSuccess('');
    try {
      await userService.changePassword({ currentPassword, newPassword });
      setPwdSuccess('Password changed successfully.');
      setTimeout(() => {
        setIsPasswordModalOpen(false);
        setCurrentPassword('');
        setNewPassword('');
        setPwdSuccess('');
      }, 2000);
    } catch (err: any) {
      setPwdError(err.response?.data?.message || 'Failed to change password');
    } finally {
      setIsChangingPwd(false);
    }
  };

  const handleDeleteAccount = async () => {
    if (confirmUsername !== user?.username) return;
    setIsDeleting(true);
    setDeleteError('');
    try {
      await userService.deleteAccount();
      await logout();
    } catch (err: any) {
      setDeleteError(err.response?.data?.message || 'Failed to delete account.');
      setIsDeleting(false);
    }
  };

  if (loading) {
    return <div className="p-8 text-white">Loading profile...</div>;
  }

  return (
    <div className="max-w-5xl mx-auto pb-12 text-arena-text">
      {/* Profile Banner Wrapper Container */}
      <div className="relative mb-16 sm:mb-20">
        {/* Banner Background Box */}
        <div
          className="h-56 sm:h-72 rounded-[14px] bg-arena-surface bg-cover bg-center overflow-hidden shadow-2xl relative border border-white/10"
          style={{
            backgroundImage: formData.bannerUrl
              ? `linear-gradient(to top, rgba(0,0,0,0.85), rgba(0,0,0,0.2)), url(${formData.bannerUrl})`
              : 'linear-gradient(135deg, #0F141C 0%, #161B22 100%)'
          }}
        >
          {/* Banner Upload Button Overlay */}
          <label className="absolute top-4 right-4 bg-black/60 hover:bg-black/80 backdrop-blur-md text-white px-3.5 py-2 rounded-[14px] font-mono text-xs font-bold cursor-pointer transition-all flex items-center gap-2 border border-white/20">
            <Camera size={16} className="text-arena-cyan" />
            <span>{formData.bannerUrl ? 'Change Banner' : 'Upload Banner'}</span>
            <input type="file" accept="image/*" onChange={handleBannerFileUpload} className="hidden" />
          </label>
        </div>

        {/* User Info & Avatar Overlay floating over bottom edge */}
        <div className="absolute left-6 -bottom-10 sm:-bottom-12 flex items-end gap-5 z-20">
          <div className="relative group/avatar">
            <div className="w-24 h-24 sm:w-28 sm:h-28 rounded-full bg-arena-cyan border-4 border-[#0B0E14] shadow-2xl overflow-hidden flex items-center justify-center text-black font-bold text-3xl shrink-0">
              {formData.avatarUrl ? (
                <img src={formData.avatarUrl} alt={formData.username} className="w-full h-full object-cover" />
              ) : (
                (formData.username || 'U').charAt(0).toUpperCase()
              )}
            </div>

            <label className="absolute inset-0 bg-black/60 rounded-full opacity-0 group-hover/avatar:opacity-100 flex items-center justify-center cursor-pointer transition-opacity">
              <Camera size={22} className="text-white" />
              <input type="file" accept="image/*" onChange={handleAvatarFileUpload} className="hidden" />
            </label>
          </div>

          <div className="mb-2">
            <h1 className="text-2xl sm:text-3xl font-extrabold text-white tracking-tight drop-shadow-md">
              {formData.displayName || formData.username}
            </h1>
            <span className="text-xs font-mono text-arena-cyan font-bold capitalize">
              {user?.role} Account
            </span>
          </div>
        </div>
      </div>

      <div className="px-4 sm:px-6 space-y-8">
        {/* Profile Pictures & Media Inputs */}
        <section>
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-lg font-bold text-white">Profile</h2>
            <button
              type="button"
              onClick={() => saveMedia()}
              disabled={saving}
              className="bg-arena-cyan hover:bg-arena-cyanHover text-black font-bold py-2 px-5 rounded-[14px] transition-all text-xs uppercase tracking-wider flex items-center gap-2 shadow-[0_0_15px_rgba(0,184,252,0.3)] disabled:opacity-50"
            >
              {saving ? 'Uploading...' : 'UPLOAD'}
            </button>
          </div>
          <div className="bg-arena-surface rounded-[14px] p-6 space-y-5 border border-arena-border">
            <div>
              <label className="block text-xs font-mono font-bold text-arena-textMuted uppercase mb-1.5">
                Profile Picture
              </label>
              <div className="flex items-center gap-3">
                <input
                  type="text"
                  name="avatarUrl"
                  value={formData.avatarUrl}
                  onChange={handleChange}
                  placeholder="Enter image URL or select file..."
                  className="flex-1 bg-black rounded-[14px] px-4 py-2.5 text-sm text-white focus:ring-1 focus:ring-arena-cyan outline-none border border-arena-border"
                />
                <label className="bg-[var(--panel-2)] hover:bg-zinc-800 text-white font-bold py-2.5 px-4 rounded-[14px] transition-all text-xs uppercase tracking-wider cursor-pointer shrink-0 flex items-center gap-2 border border-arena-border">
                  <Upload size={14} />
                  Choose File
                  <input type="file" accept="image/*" onChange={handleAvatarFileUpload} className="hidden" />
                </label>
              </div>
            </div>

            <div>
              <label className="block text-xs font-mono font-bold text-arena-textMuted uppercase mb-1.5">
                Profile Banner
              </label>
              <div className="flex items-center gap-3">
                <input
                  type="text"
                  name="bannerUrl"
                  value={formData.bannerUrl}
                  onChange={handleChange}
                  placeholder="Enter banner URL or select file..."
                  className="flex-1 bg-black rounded-[14px] px-4 py-2.5 text-sm text-white focus:ring-1 focus:ring-arena-cyan outline-none border border-arena-border"
                />
                <label className="bg-[var(--panel-2)] hover:bg-zinc-800 text-white font-bold py-2.5 px-4 rounded-[14px] transition-all text-xs uppercase tracking-wider cursor-pointer shrink-0 flex items-center gap-2 border border-arena-border">
                  <ImageIcon size={14} />
                  Choose File
                  <input type="file" accept="image/*" onChange={handleBannerFileUpload} className="hidden" />
                </label>
              </div>
            </div>
          </div>
        </section>

        {/* Profile Settings Section */}
        <section>
          <h2 className="text-xl font-bold text-white mb-4">Profile Settings</h2>
          <p className="text-sm text-arena-textMuted mb-4">Change identifying details for your account</p>

          <form onSubmit={handleSubmit} className="bg-arena-surface border border-arena-border rounded-lg p-6 space-y-6">

            {error && <div className="p-3 bg-red-900/50 border border-red-500 rounded text-red-200">{error}</div>}
            {success && <div className="p-3 bg-green-900/50 border border-green-500 rounded text-green-200">{success}</div>}

            <div className="flex flex-col md:flex-row gap-4 md:items-start">
              <label className="w-48 font-bold text-white shrink-0 mt-2">Username</label>
              <div className="flex-1 space-y-1">
                <input
                  type="text"
                  name="username"
                  value={formData.username}
                  onChange={handleChange}
                  className="w-full bg-black border border-arena-border rounded px-4 py-2 focus:border-arena-cyanFocus outline-none opacity-50 cursor-not-allowed"
                  disabled
                />
                <p className="text-xs text-arena-textMuted">You may update your username again in 2 months (Mocked - currently disabled)</p>
              </div>
            </div>

            <div className="flex flex-col md:flex-row gap-4 md:items-start">
              <label className="w-48 font-bold text-white shrink-0 mt-2">Display Name</label>
              <div className="flex-1 space-y-1">
                <input
                  type="text"
                  name="displayName"
                  value={formData.displayName}
                  onChange={handleChange}
                  className="w-full bg-black border border-arena-border rounded px-4 py-2 focus:border-arena-cyanFocus outline-none"
                />
                <p className="text-xs text-arena-textMuted">Customize capitalization for your username</p>
              </div>
            </div>

            <div className="flex flex-col md:flex-row gap-4 md:items-start">
              <label className="w-48 font-bold text-white shrink-0 mt-2">Bio</label>
              <div className="flex-1 space-y-1">
                <textarea
                  name="bio"
                  value={formData.bio}
                  onChange={handleChange}
                  rows={4}
                  className="w-full bg-black border border-arena-border rounded px-4 py-2 focus:border-arena-cyanFocus outline-none resize-none"
                />
                <p className="text-xs text-arena-textMuted">Description for the About panel on your channel page in under 300 characters</p>
              </div>
            </div>

            <div className="flex justify-between items-center pt-4">
              <button
                type="button"
                onClick={handleChangeRole}
                disabled={saving}
                className="bg-arena-cyan/10 hover:bg-arena-cyan/20 text-arena-cyan font-bold py-2 px-4 rounded transition-colors disabled:opacity-50"
              >
                {user?.role === 'Viewer' ? 'Upgrade to Organizer' : 'Revert to Viewer'}
              </button>
              <button
                type="submit"
                disabled={saving}
                className="bg-arena-border hover:bg-gray-600 text-white font-bold py-2 px-4 rounded transition-colors disabled:opacity-50"
              >
                {saving ? 'Saving...' : 'Save Changes'}
              </button>
            </div>
          </form>
        </section>

        {/* Security & Password Section */}
        <section>
          <h2 className="text-xl font-bold text-white mb-4">Security</h2>
          <div className="bg-arena-surface border border-arena-border rounded-lg p-6 flex flex-col sm:flex-row sm:items-center justify-between gap-4">
            <div>
              <h3 className="font-bold text-white">Change Password</h3>
              <p className="text-sm text-arena-textMuted">Update your password to keep your account secure.</p>
            </div>
            <button
              onClick={() => {
                setPwdError('');
                setPwdSuccess('');
                setCurrentPassword('');
                setNewPassword('');
                setIsPasswordModalOpen(true);
              }}
              className="bg-arena-border hover:bg-gray-600 text-white font-bold py-2 px-4 rounded transition-colors shrink-0"
            >
              Change Password
            </button>
          </div>
        </section>

        {/* Deactivate Account Section */}
        <section>
          <h2 className="text-xl font-bold text-white mb-4">Deactivate Your Arena Account</h2>
          <div className="bg-arena-surface border border-arena-border rounded-lg mb-8">
            <div className="flex flex-col sm:flex-row p-6 border-b border-arena-border hover:bg-arena-surfaceHover transition-colors">
              <div className="w-48 shrink-0 font-bold text-white mb-2 sm:mb-0">Disable Your Arena Account</div>
              <div className="text-sm text-arena-textMuted">
                If you want to disable your Arena account, you can do so from the <a href="#" className="text-arena-cyan hover:underline">Disable Account</a> page.
              </div>
            </div>
            <div className="flex flex-col sm:flex-row p-6 hover:bg-arena-surfaceHover transition-colors justify-between items-start sm:items-center gap-4">
              <div>
                <div className="font-bold text-white mb-1">Delete Your Arena Account</div>
                <div className="text-sm text-arena-textMuted">
                  Permanently erase all your content and profile information.
                </div>
              </div>
              <button
                onClick={() => {
                  setDeleteError('');
                  setConfirmUsername('');
                  setIsDeleteModalOpen(true);
                }}
                className="bg-red-600 hover:bg-red-700 text-white font-bold py-2 px-4 rounded transition-colors shrink-0"
              >
                Delete Account
              </button>
            </div>
          </div>
        </section>

      </div>

      {/* Change Password Modal */}
      {isPasswordModalOpen && (
        <div className="fixed inset-0 bg-black/70 flex items-center justify-center z-50 p-4">
          <div className="bg-arena-surface border border-arena-border rounded-lg p-6 max-w-md w-full">
            <h2 className="text-xl font-bold text-white mb-4">Change Password</h2>
            {pwdError && <div className="mb-4 p-3 bg-red-900/50 border border-red-500 rounded text-red-200">{pwdError}</div>}
            {pwdSuccess && <div className="mb-4 p-3 bg-green-900/50 border border-green-500 rounded text-green-200">{pwdSuccess}</div>}

            <div className="space-y-4">
              <div>
                <label className="block text-sm font-bold text-white mb-1">Current Password</label>
                <input
                  type="password"
                  value={currentPassword}
                  onChange={(e) => setCurrentPassword(e.target.value)}
                  className="w-full bg-black border border-arena-border rounded px-4 py-2 focus:border-arena-cyanFocus outline-none"
                />
              </div>
              <div>
                <label className="block text-sm font-bold text-white mb-1">New Password</label>
                <input
                  type="password"
                  value={newPassword}
                  onChange={(e) => setNewPassword(e.target.value)}
                  className="w-full bg-black border border-arena-border rounded px-4 py-2 focus:border-arena-cyanFocus outline-none"
                />
              </div>
              <div className="flex justify-end gap-3 pt-4">
                <button
                  onClick={() => setIsPasswordModalOpen(false)}
                  className="text-gray-400 hover:text-white transition-colors px-3"
                >
                  Cancel
                </button>
                <button
                  onClick={handleChangePassword}
                  disabled={isChangingPwd || !currentPassword || !newPassword}
                  className="bg-arena-cyan hover:bg-arena-cyanHover text-black font-bold py-2 px-4 rounded transition-colors disabled:opacity-50"
                >
                  {isChangingPwd ? 'Saving...' : 'Update Password'}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Delete Account Modal */}
      {isDeleteModalOpen && (
        <div className="fixed inset-0 bg-black/70 flex items-center justify-center z-50 p-4">
          <div className="bg-arena-surface border border-red-900/50 rounded-lg p-6 max-w-md w-full">
            <h2 className="text-xl font-bold text-red-500 mb-2">Delete Account</h2>
            <p className="text-sm text-gray-300 mb-4">
              This will permanently erase all your content and profile information.
              Please type <span className="font-bold text-white">{user?.username}</span> to confirm.
            </p>
            {deleteError && <div className="mb-4 p-3 bg-red-900/50 border border-red-500 rounded text-red-200">{deleteError}</div>}

            <input
              type="text"
              value={confirmUsername}
              onChange={(e) => setConfirmUsername(e.target.value)}
              placeholder="Username"
              className="w-full bg-black border border-arena-border rounded px-4 py-2 focus:border-red-500 outline-none text-white mb-6"
            />

            <div className="flex justify-end gap-3">
              <button
                onClick={() => setIsDeleteModalOpen(false)}
                className="text-gray-400 hover:text-white transition-colors px-3"
              >
                Cancel
              </button>
              <button
                onClick={handleDeleteAccount}
                disabled={isDeleting || confirmUsername !== user?.username}
                className="bg-red-600 hover:bg-red-700 text-white font-bold py-2 px-4 rounded transition-colors disabled:opacity-50"
              >
                {isDeleting ? 'Deleting...' : 'Delete Permanently'}
              </button>
            </div>
          </div>
        </div>
      )}

    </div>
  );
};

export default ProfileSettingsView;
