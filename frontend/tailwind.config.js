/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{vue,ts,js}'],
  theme: {
    extend: {
      colors: {
        night: '#001F33',
        primary: '#003B5C',
        accent: '#FFD633',
        critical: '#E53935',
        warning: '#FB8C00',
        safe: '#2E7D32'
      }
    }
  },
  plugins: []
}
