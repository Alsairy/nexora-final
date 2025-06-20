import React, { createContext, useContext, useState, useEffect, ReactNode } from 'react';
import { createTheme, ThemeProvider } from '@mui/material/styles';

interface LanguageContextType {
  language: 'en' | 'ar';
  setLanguage: (lang: 'en' | 'ar') => void;
  t: (key: string, params?: Record<string, any>) => string;
  isRTL: boolean;
}

const LanguageContext = createContext<LanguageContextType | undefined>(undefined);

interface LanguageProviderProps {
  children: ReactNode;
}

const translations: Record<string, any> = {};

const loadTranslations = async (lang: 'en' | 'ar') => {
  if (!translations[lang]) {
    try {
      const [common, esignature] = await Promise.all([
        import(`../locales/${lang}/common.json`),
        import(`../locales/${lang}/esignature.json`)
      ]);
      
      translations[lang] = {
        ...common.default,
        ...esignature.default
      };
    } catch (error) {
      console.error(`Failed to load translations for ${lang}:`, error);
      translations[lang] = {};
    }
  }
  return translations[lang];
};



export const LanguageProvider: React.FC<LanguageProviderProps> = ({ children }) => {
  const [language, setLanguageState] = useState<'en' | 'ar'>('en');
  const [translationData, setTranslationData] = useState<any>({});
  const [isLoading, setIsLoading] = useState(true);

  const isRTL = language === 'ar';

  useEffect(() => {
    const savedLanguage = localStorage.getItem('nexora-language') as 'en' | 'ar' | null;
    if (savedLanguage && ['en', 'ar'].includes(savedLanguage)) {
      setLanguageState(savedLanguage);
    }
  }, []);

  useEffect(() => {
    const loadLanguageData = async () => {
      setIsLoading(true);
      const data = await loadTranslations(language);
      setTranslationData(data);
      setIsLoading(false);
    };

    loadLanguageData();
  }, [language]);

  const setLanguage = (lang: 'en' | 'ar') => {
    setLanguageState(lang);
    localStorage.setItem('nexora-language', lang);
    document.documentElement.dir = lang === 'ar' ? 'rtl' : 'ltr';
    document.documentElement.lang = lang;
  };

  const t = (key: string, params?: Record<string, any>): string => {
    if (isLoading || !translationData) {
      return key;
    }

    const keys = key.split('.');
    let value = translationData;

    for (const k of keys) {
      if (value && typeof value === 'object' && k in value) {
        value = value[k];
      } else {
        return key;
      }
    }

    if (typeof value !== 'string') {
      return key;
    }

    if (params) {
      return value.replace(/\{(\w+)\}/g, (match, paramKey) => {
        return params[paramKey] !== undefined ? String(params[paramKey]) : match;
      });
    }

    return value;
  };

  const theme = createTheme({
    direction: isRTL ? 'rtl' : 'ltr',
    typography: {
      fontFamily: isRTL 
        ? '"Noto Sans Arabic", "Roboto", "Helvetica", "Arial", sans-serif'
        : '"Roboto", "Helvetica", "Arial", sans-serif',
    },
    palette: {
      primary: {
        main: '#1976d2',
      },
      secondary: {
        main: '#dc004e',
      },
    },
    components: {
      MuiTextField: {
        defaultProps: {
          variant: 'outlined',
        },
      },
      MuiButton: {
        styleOverrides: {
          root: {
            textTransform: 'none',
          },
        },
      },
      MuiTableCell: {
        styleOverrides: {
          root: {
            textAlign: isRTL ? 'right' : 'left',
          },
        },
      },
    },
  });

  return (
    <LanguageContext.Provider value={{ language, setLanguage, t, isRTL }}>
      <ThemeProvider theme={theme}>
        {children}
      </ThemeProvider>
    </LanguageContext.Provider>
  );
};

export const useLanguage = (): LanguageContextType => {
  const context = useContext(LanguageContext);
  if (!context) {
    throw new Error('useLanguage must be used within a LanguageProvider');
  }
  return context;
};
