// i18n.test.tsx — test song ngữ: hai từ điển cùng bộ khóa, nút đổi ngôn ngữ, chữ có sẵn của antd.
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { DatePicker } from "antd";
import enUS from "antd/locale/en_US";
import viVN from "antd/locale/vi_VN";
import { describe, expect, it } from "vitest";
import { changeAppLanguage } from "@/i18n/i18n";
import en from "@/i18n/locales/en.json";
import vi from "@/i18n/locales/vi.json";
import { renderApp } from "@/test/renderApp";
import { ThemeProvider } from "@/theme/ThemeProvider";

// Trải phẳng từ điển thành danh sách khóa: { home: { title } } → ["home.title"].
function collectKeys(dictionary: object, prefix = ""): string[] {
  const keys: string[] = [];
  for (const [key, value] of Object.entries(dictionary)) {
    const fullKey = prefix === "" ? key : `${prefix}.${key}`;
    if (typeof value === "object" && value !== null) {
      keys.push(...collectKeys(value, fullKey));
    } else {
      keys.push(fullKey);
    }
  }

  return keys.sort();
}

describe("i18n", () => {
  it("vi.json và en.json có cùng bộ khóa", () => {
    expect(collectKeys(en)).toEqual(collectKeys(vi));
  });

  it("bấm nút thì đổi ngôn ngữ, đổi <html lang> và lưu lựa chọn", async () => {
    const user = userEvent.setup();
    renderApp();
    expect(screen.getByRole("heading", { name: vi.home.title })).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: vi.shell.switchLanguage }));

    expect(await screen.findByRole("heading", { name: en.home.title })).toBeInTheDocument();
    expect(document.documentElement.lang).toBe("en");
    expect(window.localStorage.getItem("confhub.lang")).toBe("en");
  });

  it("chữ có sẵn của antd đổi theo ngôn ngữ", async () => {
    render(
      <ThemeProvider>
        <DatePicker />
      </ThemeProvider>,
    );
    const viPlaceholder = viVN.DatePicker?.lang.placeholder ?? "";
    const enPlaceholder = enUS.DatePicker?.lang.placeholder ?? "";
    expect(screen.getByPlaceholderText(viPlaceholder)).toBeInTheDocument();

    await changeAppLanguage("en");

    expect(await screen.findByPlaceholderText(enPlaceholder)).toBeInTheDocument();
  });
});
