// Forwards /api to the ASP.NET Core backend so the browser sees one origin
// and the Identity cookie works without CORS. Override with API_TARGET.
export default {
  '/api': {
    target: process.env.API_TARGET ?? 'http://localhost:5101',
    secure: false,
    changeOrigin: false,
  },
};
