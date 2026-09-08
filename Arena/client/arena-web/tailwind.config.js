/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      colors: {
        arena: {
          bg: '#000000', // OLED Void
          surface: '#0D1117', // Deep Carbon
          surfaceHover: '#161B22', // Midnight Tint
          border: '#2A2F38', // Gunmetal Stroke
          borderFocus: '#00B8FC', // Cyan Focus
          cyan: '#00B8FC', // Logitech Cyan
          cyanHover: '#009ADB',
          cyanActive: '#007BB0',
          crimson: '#FF2B56', // Crimson Pulse
          crimsonHover: '#E0224A',
          textMuted: '#71717A'
        }
      },
      fontFamily: {
        display: ['Orbitron', 'Syne', 'sans-serif'],
        sans: ['Inter', 'sans-serif'],
        mono: ['JetBrains Mono', 'Space Mono', 'monospace'],
      }
    },
  },
  plugins: [],
}
