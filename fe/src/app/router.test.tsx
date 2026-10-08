// router.test.tsx — test route: trang chủ trong Shell B, địa chỉ lạ hiện 404 và quay về được.
import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import vi from "@/i18n/locales/vi.json";
import { renderApp } from "@/test/renderApp";

describe("router", () => {
  it("'/' hiện trang chủ bên trong Shell B (có header)", () => {
    renderApp("/");

    expect(screen.getByRole("link", { name: vi.app.name })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: vi.home.title })).toBeInTheDocument();
  });

  it("địa chỉ lạ hiện trang 404, bấm nút thì về trang chủ", async () => {
    const user = userEvent.setup();
    renderApp("/khong-co-trang-nay");
    expect(screen.getByText(vi.notFound.title)).toBeInTheDocument();
    expect(screen.getByRole("link", { name: vi.app.name })).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: vi.notFound.backHome }));

    expect(await screen.findByRole("heading", { name: vi.home.title })).toBeInTheDocument();
  });
});
