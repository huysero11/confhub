// ThemeProvider.test.tsx — test chế độ sáng / tối: mặc định theo hệ điều hành, đổi được, nhớ lựa chọn.
// Kiểm qua thuộc tính <html data-theme> và localStorage (khóa "confhub.theme").
import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import { mockMatchMedia } from "@/test/mockMatchMedia";
import { renderApp } from "@/test/renderApp";

describe("ThemeProvider", () => {
  it("lần đầu theo nền của hệ điều hành", () => {
    mockMatchMedia(true);

    renderApp();

    expect(document.documentElement.dataset.theme).toBe("dark");
  });

  it("bấm nút thì đổi nền và lưu lựa chọn", async () => {
    const user = userEvent.setup();
    renderApp();
    expect(document.documentElement.dataset.theme).toBe("light");

    await user.click(screen.getByRole("button", { name: "Chuyển sang nền tối" }));

    expect(document.documentElement.dataset.theme).toBe("dark");
    expect(window.localStorage.getItem("confhub.theme")).toBe("dark");
    expect(screen.getByRole("button", { name: "Chuyển sang nền sáng" })).toBeInTheDocument();
  });

  it("đã lưu lựa chọn thì dùng lựa chọn đó, bỏ qua hệ điều hành", () => {
    mockMatchMedia(true);
    window.localStorage.setItem("confhub.theme", "light");

    renderApp();

    expect(document.documentElement.dataset.theme).toBe("light");
  });
});
