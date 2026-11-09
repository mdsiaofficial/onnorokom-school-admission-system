/**
 * Formats an ISO date string in Asia/Dhaka time zone with Latin/English numerals (0-9)
 */
export const formatDhakaDateTime = (
  isoString: string | null | undefined,
  lang: 'bn' | 'en' = 'bn'
): string => {
  if (!isoString) return '-';
  try {
    const date = new Date(isoString);
    if (Number.isNaN(date.getTime())) return '-';

    const locale = lang === 'bn' ? 'bn-BD-u-nu-latn' : 'en-GB';

    return new Intl.DateTimeFormat(locale, {
      timeZone: 'Asia/Dhaka',
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
      hour12: true,
    }).format(date);
  } catch {
    return isoString;
  }
};

/**
 * Formats time only in Asia/Dhaka time zone with Latin numerals
 */
export const formatDhakaTime = (
  date: Date = new Date(),
  lang: 'bn' | 'en' = 'bn'
): string => {
  try {
    const locale = lang === 'bn' ? 'bn-BD-u-nu-latn' : 'en-GB';
    return new Intl.DateTimeFormat(locale, {
      timeZone: 'Asia/Dhaka',
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
      hour12: true,
    }).format(date);
  } catch {
    return date.toLocaleTimeString();
  }
};

/**
 * Format numbers using Latin numerals (0-9)
 */
export const formatNumber = (num: number): string => {
  return new Intl.NumberFormat('en-US').format(num);
};
