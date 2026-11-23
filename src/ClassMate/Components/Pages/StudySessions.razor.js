// Returns the browser's IANA time zone, e.g. "America/Denver", so the
// Study Sessions page can tell what "now" means for the student.
export function getTimeZone() {
    return Intl.DateTimeFormat().resolvedOptions().timeZone;
}
