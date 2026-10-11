import React, { useEffect, useState, useCallback } from 'react';
import {
  Swords,
  Sword,
  Flame,
  Zap,
  Crosshair,
  Hammer,
  Coins,
  AlertCircle,
  Sparkles,
  Loader2,
  ShieldAlert,
} from 'lucide-react';
import { Weapon } from '../../types';
import { weaponShopService } from '../../api/weaponShopService';
import { useWallet } from '../../context/WalletContext';
import { useNotification } from '../../context/NotificationContext';
import { useAuth } from '../../context/AuthContext';

interface WeaponShopProps {
  matchId: number;
  teamId: number | null;
}

const getWeaponIcon = (iconKey: string) => {
  switch (iconKey.toLowerCase()) {
    case 'knife':
      return <Sword size={20} className="text-cyan-400" />;
    case 'crossbow':
      return <Crosshair size={20} className="text-emerald-400" />;
    case 'hammer':
      return <Hammer size={20} className="text-orange-400" />;
    case 'dragon':
      return <Flame size={20} className="text-rose-400" />;
    case 'lightning':
      return <Zap size={20} className="text-amber-400" />;
    default:
      return <Swords size={20} className="text-arena-cyan" />;
  }
};

export const WeaponShop: React.FC<WeaponShopProps> = ({ matchId, teamId }) => {
  const { user } = useAuth();
  const { balance, optimisticSpend, updateBalance } = useWallet();
  const { notify } = useNotification();

  const [weapons, setWeapons] = useState<Weapon[]>([]);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);
  const [purchasingWeaponId, setPurchasingWeaponId] = useState<number | null>(null);
  const [lastAttackEffect, setLastAttackEffect] = useState<{
    weaponId: number;
    damage: number;
  } | null>(null);

  const fetchWeapons = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      const data = await weaponShopService.getWeapons();
      setWeapons(data);
    } catch (err: unknown) {
      console.error('Failed to load weapon catalog:', err);
      setError('Unable to load weapons. Please try again.');
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchWeapons();
  }, [fetchWeapons]);

  const handleBuy = async (weapon: Weapon) => {
    if (!user) {
      notify('Please log in to purchase weapons and attack.', 'info');
      return;
    }

    if (teamId === null || teamId <= 0) {
      notify('Please select a faction team above before launching an attack.', 'info');
      return;
    }

    if (balance < weapon.cost) {
      notify(`Insufficient coins! You need ${weapon.cost} coins for ${weapon.name}.`, 'error');
      return;
    }

    // Optimistic coin deduction
    const { success, rollback } = optimisticSpend(weapon.cost);
    if (!success) {
      notify('Insufficient coins to purchase this weapon.', 'error');
      return;
    }

    setPurchasingWeaponId(weapon.weaponId);

    try {
      const response = await weaponShopService.purchaseAttack({
        weaponId: weapon.weaponId,
        matchId,
        teamId,
      });

      if (response.success) {
        updateBalance(response.currentBalance);
        notify(response.message || `${weapon.name} attack launched!`, 'success');
        setLastAttackEffect({ weaponId: weapon.weaponId, damage: weapon.damage });
        setTimeout(() => setLastAttackEffect(null), 2000);
      } else {
        rollback();
        notify(response.message || 'Attack submission failed.', 'error');
      }
    } catch (err: any) {
      rollback();
      const serverMessage = err?.response?.data?.message || err?.message || 'Attack purchase failed.';
      notify(serverMessage, 'error');
    } finally {
      setPurchasingWeaponId(null);
    }
  };

  return (
    <div
      data-testid="weapon-shop"
      className="w-full bg-[var(--panel)] border border-[var(--line)] rounded-xl p-4 flex flex-col gap-4 shadow-lg backdrop-blur-md"
    >
      {/* Header */}
      <div className="flex items-center justify-between border-b border-[var(--line)] pb-3">
        <div className="flex items-center gap-2">
          <div className="p-2 rounded-lg bg-arena-cyan/10 border border-arena-cyan/30 text-arena-cyan">
            <Swords size={20} />
          </div>
          <div>
            <h3 className="text-base font-black tracking-wide text-arena-text uppercase flex items-center gap-1.5">
              Weapon Arsenal
              <Sparkles size={14} className="text-amber-400" />
            </h3>
            <p className="text-xs text-arena-textMuted">
              Spend coins to launch attacks for your team
            </p>
          </div>
        </div>

        <div className="flex items-center gap-1.5 px-3 py-1 bg-black/40 border border-amber-400/20 rounded-full text-amber-400 text-xs font-bold">
          <Coins size={13} className="text-amber-400" />
          <span>{balance.toLocaleString()} coins</span>
        </div>
      </div>

      {/* Team Selection Warning Banner */}
      {teamId === null && (
        <div
          data-testid="no-team-warning"
          className="flex items-center gap-2.5 p-3 rounded-lg bg-amber-500/10 border border-amber-500/30 text-amber-400 text-xs font-semibold"
        >
          <ShieldAlert size={16} className="shrink-0 text-amber-400" />
          <span>Select a faction team above to unlock weapon attacks for this match.</span>
        </div>
      )}

      {/* Loading Skeleton */}
      {isLoading && (
        <div data-testid="weapon-shop-loading" className="flex flex-col items-center justify-center py-10 gap-3 text-arena-textMuted">
          <Loader2 size={28} className="animate-spin text-arena-cyan" />
          <span className="text-xs uppercase tracking-wider font-semibold">Loading Armory...</span>
        </div>
      )}

      {/* Error State */}
      {!isLoading && error && (
        <div className="flex flex-col items-center justify-center py-8 gap-3 text-center">
          <AlertCircle size={26} className="text-rose-400" />
          <p className="text-xs text-rose-300">{error}</p>
          <button
            onClick={fetchWeapons}
            className="px-3 py-1.5 rounded-lg bg-arena-cyan/20 border border-arena-cyan/40 text-arena-cyan text-xs font-bold hover:bg-arena-cyan/30 transition-colors"
          >
            Retry
          </button>
        </div>
      )}

      {/* Weapons Catalog Grid */}
      {!isLoading && !error && weapons.length === 0 && (
        <div className="text-center py-8 text-xs text-arena-textMuted font-medium">
          No weapons are currently available in the armory.
        </div>
      )}

      {!isLoading && !error && weapons.length > 0 && (
        <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-5 gap-3.5">
          {weapons.map((weapon) => {
            const hasEnoughCoins = balance >= weapon.cost;
            const hasSelectedTeam = teamId !== null && teamId > 0;
            const canAfford = hasEnoughCoins && hasSelectedTeam;
            const isPurchasing = purchasingWeaponId === weapon.weaponId;
            const isAnyPurchasing = purchasingWeaponId !== null;
            const isEffectActive = lastAttackEffect?.weaponId === weapon.weaponId;

            return (
              <div
                key={weapon.weaponId}
                data-testid={`weapon-card-${weapon.weaponId}`}
                className={`relative flex flex-col justify-between p-3 rounded-xl border transition-all duration-200 ${
                  isEffectActive
                    ? 'ring-2 ring-rose-500 bg-rose-500/10 border-rose-500/50 shadow-[0_0_15px_rgba(244,63,94,0.3)]'
                    : canAfford
                    ? 'bg-black/30 border-[var(--line)] hover:border-arena-cyan/50 hover:bg-black/40'
                    : 'bg-black/20 border-[var(--line)]/50 opacity-60'
                }`}
              >
                {/* Attack Particle / Feedback Animation Banner */}
                {isEffectActive && (
                  <div className="absolute inset-0 bg-rose-500/20 backdrop-blur-xs flex items-center justify-center rounded-xl z-10 animate-pulse">
                    <span className="text-xs font-black text-rose-200 tracking-wider uppercase">
                      +{lastAttackEffect.damage} DMG Attack!
                    </span>
                  </div>
                )}

                {/* Top Info */}
                <div className="flex items-start gap-2.5 mb-2">
                  <div className="p-2 rounded-lg bg-[var(--line)]/40 shrink-0">
                    {getWeaponIcon(weapon.iconKey)}
                  </div>
                  <div className="min-w-0 flex-1">
                    <div className="flex items-center justify-between gap-1">
                      <h4 className="text-sm font-bold text-arena-text truncate" title={weapon.name}>
                        {weapon.name}
                      </h4>
                      <span className="text-[10px] font-black uppercase px-1.5 py-0.5 rounded bg-rose-500/20 text-rose-400 border border-rose-500/30 shrink-0">
                        +{weapon.damage} DMG
                      </span>
                    </div>
                    <p className="text-[11px] text-arena-textMuted line-clamp-2 mt-0.5">
                      {weapon.description}
                    </p>
                  </div>
                </div>

                {/* Bottom Bar: Cost & Attack Button */}
                <div className="flex items-center justify-between mt-auto pt-2 border-t border-[var(--line)]/40">
                  <div className="flex items-center gap-1 text-amber-400 text-xs font-bold">
                    <Coins size={13} className="text-amber-400" />
                    <span>{weapon.cost}</span>
                  </div>

                  <button
                    data-testid={`buy-weapon-${weapon.weaponId}`}
                    onClick={() => handleBuy(weapon)}
                    disabled={!canAfford || isAnyPurchasing}
                    title={
                      !hasSelectedTeam
                        ? 'Select a team first'
                        : !hasEnoughCoins
                        ? 'Not enough coins'
                        : isPurchasing
                        ? 'Attacking...'
                        : `Attack with ${weapon.name}`
                    }
                    className={`px-3 py-1.5 rounded-lg text-xs font-black uppercase tracking-wider transition-all duration-200 flex items-center gap-1.5 ${
                      canAfford && !isAnyPurchasing
                        ? 'bg-gradient-to-r from-arena-cyan to-blue-500 text-black shadow-md shadow-arena-cyan/20 hover:scale-[1.02] hover:brightness-110 active:scale-[0.98]'
                        : 'bg-white/5 text-arena-textMuted cursor-not-allowed border border-white/5'
                    }`}
                  >
                    {isPurchasing ? (
                      <>
                        <Loader2 size={12} className="animate-spin text-black" />
                        <span>ATTACKING</span>
                      </>
                    ) : (
                      <>
                        <Swords size={12} />
                        <span>ATTACK</span>
                      </>
                    )}
                  </button>
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
};

export default WeaponShop;
