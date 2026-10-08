// LanguageToggle.tsx — nút đổi ngôn ngữ vi / en.
// Chứa: một Button của antd, hiện mã của ngôn ngữ SẼ chuyển sang ("EN" khi đang ở tiếng Việt).
// Dùng ở: layouts/ShellB/ShellBHeader.tsx.
import { Button } from "antd";
import { useTranslation } from "react-i18next";
import { changeAppLanguage, toAppLanguage } from "@/i18n/i18n";

export function LanguageToggle() {
  /*
        - t: hàm tra từ khóa
        - i18n: đối tượng biết ngôn ngữ và đổi ngôn ngữ
    */
  const { t, i18n } = useTranslation();
  const currentLanguage = toAppLanguage(i18n.language);
  const nextLanguage = currentLanguage === "vi" ? "en" : "vi";
  const label = t("shell.switchLanguage");

  return (
    <Button
      type="text"
      aria-label={label}
      title={label}
      onClick={() => void changeAppLanguage(nextLanguage)}
    >
      {nextLanguage.toUpperCase()}
    </Button>
  );
}
