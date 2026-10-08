// i18n.ts — khởi tạo song ngữ vi / en cho chữ của ứng dụng (thư viện i18next).
// Chứa: kiểu AppLanguage, chọn ngôn ngữ ban đầu, hàm đổi ngôn ngữ (có lưu lại).
// Dùng ở: main.tsx import 1 lần để khởi tạo; component lấy chữ qua hook useTranslation().
import i18n from "i18next";
import { initReactI18next } from "react-i18next";
import en from "@/i18n/locales/en.json";
import vi from "@/i18n/locales/vi.json";

export type AppLanguage = "vi" | "en";

const LANGUAGE_STORAGE_KEY = "confhub.lang";

// i18next trả ngôn ngữ dạng string; đưa về đúng 2 giá trị ứng dụng hỗ trợ.
export function toAppLanguage(language: string): AppLanguage {
  return language === "en" ? "en" : "vi";
}

// Đã từng chọn: dùng lựa chọn đã lưu. Lần đầu: trình duyệt tiếng Việt thì "vi", còn lại "en".
function readInitialLanguage(): AppLanguage {
  const savedLanguage = window.localStorage.getItem(LANGUAGE_STORAGE_KEY);
  if (savedLanguage === "vi" || savedLanguage === "en") {
    return savedLanguage;
  }

  const isVietnameseBrowser = window.navigator.language.toLowerCase().startsWith("vi");
  return isVietnameseBrowser ? "vi" : "en";
}

const initialLanguage = readInitialLanguage();

// Nối i18next với React, để hook useTranslation dùng được.
void i18n.use(initReactI18next).init({
  // Nạp 2 file vào bộ nhớ để chuyển ngôn ngữ thì tra
  resources: {
    vi: { translation: vi },
    en: { translation: en },
  },
  lng: initialLanguage,
  fallbackLng: "vi",
  // React đã tự chống chèn HTML, không cần i18next escape thêm lần nữa.
  interpolation: { escapeValue: false },
});

document.documentElement.lang = initialLanguage;

// Đổi ngôn ngữ do người dùng bấm nút: lưu lại và cập nhật <html lang>.
export async function changeAppLanguage(language: AppLanguage): Promise<void> {
  window.localStorage.setItem(LANGUAGE_STORAGE_KEY, language);
  document.documentElement.lang = language;
  await i18n.changeLanguage(language);
}
