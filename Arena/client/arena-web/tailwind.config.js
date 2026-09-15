/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  darkMode: 'class',
  theme: {
    extend: {
      colors: {
        arena: {
          bg: 'var(--bg)', // OLED Void
          surface: 'var(--panel)', // Deep Carbon
          surfaceHover: 'var(--panel-2)', // Midnight Tint
          border: 'var(--line)', // Gunmetal Stroke
          borderFocus: 'var(--line-active)', // Cyan Focus
          cyan: 'var(--prime)', // Logitech Cyan
          cyanHover: 'var(--prime-dim)',
          cyanActive: '#007BB0',
          crimson: 'var(--live)', // Crimson Pulse
          crimsonHover: '#E0224A',
          textMuted: 'var(--muted)',
          text: 'var(--text)',
        }
      },
      fontFamily: {
        display: ['Roobert', 'Inter', '-apple-system', 'BlinkMacSystemFont', 'Segoe UI', 'Helvetica', 'Arial', 'sans-serif'],
        sans: ['Roobert', 'Inter', '-apple-system', 'BlinkMacSystemFont', 'Segoe UI', 'Helvetica', 'Arial', 'sans-serif'],
        mono: ['JetBrains Mono', 'Space Mono', 'monospace'],
      }
    },
  },
  plugins: [],
}
