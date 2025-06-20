import { HijriDate, convertToHijri, convertToGregorian, formatHijriDate, formatGregorianDateArabic } from './hijriCalendar';

export type CalendarType = 'gregorian' | 'hijri';

export interface CalendarConfig {
  type: CalendarType;
  language: 'en' | 'ar';
  showBothCalendars?: boolean;
}

export interface FormattedDate {
  primary: string;
  secondary?: string;
  date: Date;
  hijriDate: HijriDate;
}

export const formatDateByCalendar = (
  date: Date,
  config: CalendarConfig,
  includeDay: boolean = true
): FormattedDate => {
  const hijriDate = convertToHijri(date);
  
  let primary: string;
  let secondary: string | undefined;
  
  if (config.type === 'hijri') {
    primary = formatHijriDate(hijriDate, config.language, includeDay);
    if (config.showBothCalendars) {
      secondary = config.language === 'ar' 
        ? formatGregorianDateArabic(date, includeDay)
        : date.toLocaleDateString('en-US', {
            year: 'numeric',
            month: 'long',
            day: includeDay ? 'numeric' : undefined
          });
    }
  } else {
    if (config.language === 'ar') {
      primary = formatGregorianDateArabic(date, includeDay);
      if (config.showBothCalendars) {
        secondary = formatHijriDate(hijriDate, 'ar', includeDay);
      }
    } else {
      primary = date.toLocaleDateString('en-US', {
        year: 'numeric',
        month: 'long',
        day: includeDay ? 'numeric' : undefined
      });
      if (config.showBothCalendars) {
        secondary = formatHijriDate(hijriDate, 'en', includeDay);
      }
    }
  }
  
  return {
    primary,
    secondary,
    date,
    hijriDate
  };
};

export const getCalendarDisplayName = (type: CalendarType, language: 'en' | 'ar'): string => {
  if (language === 'ar') {
    return type === 'hijri' ? 'هجري' : 'ميلادي';
  } else {
    return type === 'hijri' ? 'Hijri' : 'Gregorian';
  }
};

export const getDateRangeText = (
  startDate: Date,
  endDate: Date,
  config: CalendarConfig
): string => {
  const start = formatDateByCalendar(startDate, config);
  const end = formatDateByCalendar(endDate, config);
  
  if (config.language === 'ar') {
    return `من ${start.primary} إلى ${end.primary}`;
  } else {
    return `From ${start.primary} to ${end.primary}`;
  }
};

export const isDateInRange = (
  date: Date,
  startDate: Date,
  endDate: Date
): boolean => {
  return date >= startDate && date <= endDate;
};

export const getMonthStartEnd = (
  date: Date,
  calendarType: CalendarType
): { start: Date; end: Date } => {
  if (calendarType === 'gregorian') {
    const start = new Date(date.getFullYear(), date.getMonth(), 1);
    const end = new Date(date.getFullYear(), date.getMonth() + 1, 0);
    return { start, end };
  } else {
    const hijriDate = convertToHijri(date);
    const monthStart: HijriDate = { ...hijriDate, day: 1 };
    const monthEnd: HijriDate = { 
      ...hijriDate, 
      day: getHijriDaysInMonth(hijriDate.year, hijriDate.month) 
    };
    
    return {
      start: convertToGregorian(monthStart),
      end: convertToGregorian(monthEnd)
    };
  }
};

export const getYearStartEnd = (
  date: Date,
  calendarType: CalendarType
): { start: Date; end: Date } => {
  if (calendarType === 'gregorian') {
    const start = new Date(date.getFullYear(), 0, 1);
    const end = new Date(date.getFullYear(), 11, 31);
    return { start, end };
  } else {
    const hijriDate = convertToHijri(date);
    const yearStart: HijriDate = { year: hijriDate.year, month: 1, day: 1 };
    const yearEnd: HijriDate = { year: hijriDate.year, month: 12, day: 29 };
    
    return {
      start: convertToGregorian(yearStart),
      end: convertToGregorian(yearEnd)
    };
  }
};

export const addCalendarMonths = (
  date: Date,
  months: number,
  calendarType: CalendarType
): Date => {
  if (calendarType === 'gregorian') {
    const newDate = new Date(date);
    newDate.setMonth(newDate.getMonth() + months);
    return newDate;
  } else {
    const hijriDate = convertToHijri(date);
    const newHijriDate = addHijriMonths(hijriDate, months);
    return convertToGregorian(newHijriDate);
  }
};

export const addCalendarYears = (
  date: Date,
  years: number,
  calendarType: CalendarType
): Date => {
  if (calendarType === 'gregorian') {
    const newDate = new Date(date);
    newDate.setFullYear(newDate.getFullYear() + years);
    return newDate;
  } else {
    const hijriDate = convertToHijri(date);
    const newHijriDate: HijriDate = { ...hijriDate, year: hijriDate.year + years };
    return convertToGregorian(newHijriDate);
  }
};

export const getCalendarMonthNames = (
  calendarType: CalendarType,
  language: 'en' | 'ar'
): string[] => {
  if (calendarType === 'hijri') {
    if (language === 'ar') {
      return [
        'محرم', 'صفر', 'ربيع الأول', 'ربيع الثاني', 'جمادى الأولى', 'جمادى الآخرة',
        'رجب', 'شعبان', 'رمضان', 'شوال', 'ذو القعدة', 'ذو الحجة'
      ];
    } else {
      return [
        'Muharram', 'Safar', 'Rabi\' al-awwal', 'Rabi\' al-thani', 
        'Jumada al-awwal', 'Jumada al-thani', 'Rajab', 'Sha\'ban', 
        'Ramadan', 'Shawwal', 'Dhu al-Qi\'dah', 'Dhu al-Hijjah'
      ];
    }
  } else {
    if (language === 'ar') {
      return [
        'يناير', 'فبراير', 'مارس', 'أبريل', 'مايو', 'يونيو',
        'يوليو', 'أغسطس', 'سبتمبر', 'أكتوبر', 'نوفمبر', 'ديسمبر'
      ];
    } else {
      return [
        'January', 'February', 'March', 'April', 'May', 'June',
        'July', 'August', 'September', 'October', 'November', 'December'
      ];
    }
  }
};

export const getCalendarDayNames = (language: 'en' | 'ar'): string[] => {
  if (language === 'ar') {
    return ['الأحد', 'الاثنين', 'الثلاثاء', 'الأربعاء', 'الخميس', 'الجمعة', 'السبت'];
  } else {
    return ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];
  }
};

export const getCalendarDayNamesShort = (language: 'en' | 'ar'): string[] => {
  if (language === 'ar') {
    return ['أحد', 'اثنين', 'ثلاثاء', 'أربعاء', 'خميس', 'جمعة', 'سبت'];
  } else {
    return ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];
  }
};

const getHijriDaysInMonth = (year: number, month: number): number => {
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

const isHijriLeapYear = (year: number): boolean => {
  return ((year * 11) + 14) % 30 < 11;
};

const addHijriMonths = (hijriDate: HijriDate, months: number): HijriDate => {
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
