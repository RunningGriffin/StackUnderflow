// Returns the browser's IANA time zone, e.g. "America/Denver", so the
// Weekly Study Goals page can tell which day "today" is for the student.
export function getTimeZone() {
    return Intl.DateTimeFormat().resolvedOptions().timeZone;
}
