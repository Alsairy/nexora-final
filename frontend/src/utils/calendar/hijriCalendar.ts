export interface HijriDate {
  year: number;
  month: number;
  day: number;
}

export interface CalendarConversionResult {
  hijri: HijriDate;
  gregorian: Date;
  isValid: boolean;
  error?: string;
}

const HIJRI_MONTHS = [
  'محرم', 'صفر', 'ربيع الأول', 'ربيع الثاني', 'جمادى الأولى', 'جمادى الآخرة',
  'رجب', 'شعبان', 'رمضان', 'شوال', 'ذو القعدة', 'ذو الحجة'
];

const HIJRI_MONTHS_EN = [
  'Muharram', 'Safar', 'Rabi\' al-awwal', 'Rabi\' al-thani', 'Jumada al-awwal', 'Jumada al-thani',
  'Rajab', 'Sha\'ban', 'Ramadan', 'Shawwal', 'Dhu al-Qi\'dah', 'Dhu al-Hijjah'
];

const GREGORIAN_MONTHS_AR = [
  'يناير', 'فبراير', 'مارس', 'أبريل', 'مايو', 'يونيو',
  'يوليو', 'أغسطس', 'سبتمبر', 'أكتوبر', 'نوفمبر', 'ديسمبر'
];

export const getHijriMonthName = (month: number, language: 'ar' | 'en' = 'ar'): string => {
  if (month < 1 || month > 12) return '';
  return language === 'ar' ? (HIJRI_MONTHS[month - 1] || '') : (HIJRI_MONTHS_EN[month - 1] || '');
};

export const getGregorianMonthNameArabic = (month: number): string => {
  if (month < 1 || month > 12) return '';
  return GREGORIAN_MONTHS_AR[month - 1] || '';
};

export const convertToHijri = (gregorianDate: Date): HijriDate => {
  const jd = gregorianToJulianDay(gregorianDate);
  return julianDayToHijri(jd);
};

export const convertToGregorian = (hijriDate: HijriDate): Date => {
  const jd = hijriToJulianDay(hijriDate);
  return julianDayToGregorian(jd);
};

export const validateHijriDate = (hijriDate: HijriDate): boolean => {
  if (!hijriDate || typeof hijriDate.year !== 'number' || typeof hijriDate.month !== 'number' || typeof hijriDate.day !== 'number') {
    return false;
  }

  if (hijriDate.year < 1 || hijriDate.year > 1500) return false;
  if (hijriDate.month < 1 || hijriDate.month > 12) return false;
  if (hijriDate.day < 1 || hijriDate.day > 30) return false;

  const daysInMonth = getHijriDaysInMonth(hijriDate.year, hijriDate.month);
  return hijriDate.day <= daysInMonth;
};

export const getHijriDaysInMonth = (year: number, month: number): number => {
  if (month < 1 || month > 12) return 0;
  
  const oddMonths = [1, 3, 5, 7, 9, 11];
  const evenMonths = [2, 4, 6, 8, 10];
  
  if (oddMonths.includes(month)) return 30;
  if (evenMonths.includes(month)) return 29;
  
  if (month === 12) {
    return isHijriLeapYear(year) ? 30 : 29;
  }
  
  return 29;
};

export const isHijriLeapYear = (year: number): boolean => {
  return ((year * 11) + 14) % 30 < 11;
};

export const formatHijriDate = (hijriDate: HijriDate, language: 'ar' | 'en' = 'ar', includeDay: boolean = true): string => {
  if (!validateHijriDate(hijriDate)) return '';
  
  const monthName = getHijriMonthName(hijriDate.month, language);
  const dayStr = language === 'ar' ? convertToArabicNumerals(hijriDate.day) : hijriDate.day.toString();
  const yearStr = language === 'ar' ? convertToArabicNumerals(hijriDate.year) : hijriDate.year.toString();
  
  if (language === 'ar') {
    return includeDay ? `${dayStr} ${monthName} ${yearStr} هـ` : `${monthName} ${yearStr} هـ`;
  } else {
    return includeDay ? `${hijriDate.day} ${monthName} ${hijriDate.year} AH` : `${monthName} ${hijriDate.year} AH`;
  }
};

export const formatGregorianDateArabic = (date: Date, includeDay: boolean = true): string => {
  const day = date.getDate();
  const month = date.getMonth() + 1;
  const year = date.getFullYear();
  
  const dayStr = convertToArabicNumerals(day);
  const monthName = getGregorianMonthNameArabic(month);
  const yearStr = convertToArabicNumerals(year);
  
  return includeDay ? `${dayStr} ${monthName} ${yearStr} م` : `${monthName} ${yearStr} م`;
};

export const convertToArabicNumerals = (num: number): string => {
  const arabicNumerals = ['٠', '١', '٢', '٣', '٤', '٥', '٦', '٧', '٨', '٩'];
  return num.toString().split('').map(digit => arabicNumerals[parseInt(digit)] || digit).join('');
};

export const convertFromArabicNumerals = (arabicNum: string): number => {
  const arabicToEnglish: { [key: string]: string } = {
    '٠': '0', '١': '1', '٢': '2', '٣': '3', '٤': '4',
    '٥': '5', '٦': '6', '٧': '7', '٨': '8', '٩': '9'
  };
  
  const englishNum = arabicNum.split('').map(char => arabicToEnglish[char] || char).join('');
  return parseInt(englishNum) || 0;
};

const gregorianToJulianDay = (date: Date): number => {
  const year = date.getFullYear();
  const month = date.getMonth() + 1;
  const day = date.getDate();
  
  let a = Math.floor((14 - month) / 12);
  let y = year - a;
  let m = month + 12 * a - 3;
  
  return day + Math.floor((153 * m + 2) / 5) + 365 * y + Math.floor(y / 4) - Math.floor(y / 100) + Math.floor(y / 400) + 1721119;
};

const julianDayToGregorian = (jd: number): Date => {
  let a = jd + 32044;
  let b = Math.floor((4 * a + 3) / 146097);
  let c = a - Math.floor((146097 * b) / 4);
  let d = Math.floor((4 * c + 3) / 1461);
  let e = c - Math.floor((1461 * d) / 4);
  let m = Math.floor((5 * e + 2) / 153);
  
  let day = e - Math.floor((153 * m + 2) / 5) + 1;
  let month = m + 3 - 12 * Math.floor(m / 10);
  let year = 100 * b + d - 4800 + Math.floor(m / 10);
  
  return new Date(year, month - 1, day);
};

const hijriToJulianDay = (hijriDate: HijriDate): number => {
  const { year, month, day } = hijriDate;
  return Math.floor((11 * year + 3) / 30) + Math.floor(354 * year) + Math.floor((30 * month - month + 1) / 2) + day + 1948440.5;
};

const julianDayToHijri = (jd: number): HijriDate => {
  const l = Math.floor(jd - 1948440.5);
  const n = Math.floor((l - 1) / 10631);
  const l2 = l - 10631 * n + 354;
  const j = Math.floor((10985 - l2) / 5316) * Math.floor((50 * l2) / 17719) + Math.floor(l2 / 5670) * Math.floor((43 * l2) / 15238);
  const l3 = l2 - Math.floor((30 - j) / 15) * Math.floor((17719 * j) / 50) - Math.floor(j / 16) * Math.floor((15238 * j) / 43) + 29;
  const m = Math.floor((24 * l3) / 709);
  const d = l3 - Math.floor((709 * m) / 24);
  const y = 30 * n + j - 30;
  
  return {
    year: y,
    month: m,
    day: d
  };
};

export const getCurrentHijriDate = (): HijriDate => {
  return convertToHijri(new Date());
};

export const addHijriDays = (hijriDate: HijriDate, days: number): HijriDate => {
  const gregorianDate = convertToGregorian(hijriDate);
  gregorianDate.setDate(gregorianDate.getDate() + days);
  return convertToHijri(gregorianDate);
};

export const addHijriMonths = (hijriDate: HijriDate, months: number): HijriDate => {
  let newMonth = hijriDate.month + months;
  let newYear = hijriDate.year;
  
  while (newMonth > 12) {
    newMonth -= 12;
    newYear++;
  }
  
  while (newMonth < 1) {
    newMonth += 12;
    newYear--;
  }
  
  const maxDaysInNewMonth = getHijriDaysInMonth(newYear, newMonth);
  const newDay = Math.min(hijriDate.day, maxDaysInNewMonth);
  
  return {
    year: newYear,
    month: newMonth,
    day: newDay
  };
};

export const compareHijriDates = (date1: HijriDate, date2: HijriDate): number => {
  if (date1.year !== date2.year) return date1.year - date2.year;
  if (date1.month !== date2.month) return date1.month - date2.month;
  return date1.day - date2.day;
};

export const isValidHijriDateString = (dateString: string): boolean => {
  const parts = dateString.split('/');
  if (parts.length !== 3) return false;
  
  const day = parseInt(parts[0] || '0');
  const month = parseInt(parts[1] || '0');
  const year = parseInt(parts[2] || '0');
  
  return validateHijriDate({ year, month, day });
};

export const parseHijriDateString = (dateString: string): HijriDate | null => {
  if (!isValidHijriDateString(dateString)) return null;
  
  const parts = dateString.split('/');
  return {
    day: parseInt(parts[0] || '0'),
    month: parseInt(parts[1] || '0'),
    year: parseInt(parts[2] || '0')
  };
};
