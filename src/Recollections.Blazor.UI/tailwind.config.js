/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    './**/*.razor',
    './wwwroot/**/*.html',
    './wwwroot/js/**/*.js'
  ],
  theme: {
    extend: {
      fontFamily: {
        sans: ['Inter', '-apple-system', 'ui-sans-serif', 'system-ui', 'sans-serif'],
      },
      colors: {
        bg: '#f5f5f7',
        accent: {
          DEFAULT: '#6366f1',
          light: '#818cf8',
          dark: '#4f46e5',
          bg: '#eef2ff',
        },
        'text-primary': '#1d1d1f',
        'text-secondary': '#6e6e73',
        'text-tertiary': '#86868b',
      },
      borderRadius: {
        '2xl': '16px',
        '3xl': '20px',
        '4xl': '28px',
      }
    },
  },
  corePlugins: {
    preflight: false,
  },
  plugins: [],
}
