// antdLocales.ts — ánh xạ ngôn ngữ của ứng dụng sang gói chữ có sẵn của antd.
// Chứa: bảng "vi" | "en" → locale của antd (chữ trong lịch, phân trang, "Không có dữ liệu"...).
// Dùng ở: ThemeProvider, truyền vào ConfigProvider.
import type { Locale } from "antd/es/locale";
import enUS from "antd/locale/en_US";
import viVN from "antd/locale/vi_VN";
import type { AppLanguage } from "@/i18n/i18n";

export const antdLocales: Record<AppLanguage, Locale> = {
  vi: viVN,
  en: enUS,
};
