// Discreet version badge shown in the corner of the app.
// Values are injected at build time (see vite.config.ts): version from
// package.json, commit/build-time from CI. Hover shows the full build metadata.
export function VersionBadge() {
  return (
    <div
      title={`commit ${__APP_COMMIT__} · built ${__APP_BUILD_TIME__}`}
      className="fixed bottom-2 left-2 z-50 select-none rounded bg-black/5 px-1.5 py-0.5 font-mono text-[10px] text-gray-400"
    >
      v{__APP_VERSION__}
    </div>
  )
}
