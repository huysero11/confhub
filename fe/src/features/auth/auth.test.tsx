// auth.test.tsx — test các luồng tài khoản qua router thật và axios giả ở lớp test.
// Gom nhiều trạng thái trong cùng use case; renderApp dùng StrictMode và QueryClient mới mỗi lần.
import { act, fireEvent, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { renderApp } from "@/test/renderApp";
import { apiMock } from "@/test/apiMock";
import { getSession } from "@/lib/authSession";
import { changeAppLanguage } from "@/i18n/i18n";
import viText from "@/i18n/locales/vi.json";
import enText from "@/i18n/locales/en.json";
import type { SessionResponse } from "@/lib/authSession";

const session: SessionResponse = {
  accessToken: "test-access-token",
  expiresIn: 900,
  user: {
    id: "user-1",
    email: "lan@example.com",
    fullName: "Mai Lan",
    role: "Organizer",
    organization: "ConfHub",
    permissions: [],
  },
};
function fillField(label: string, text: string) {
  fireEvent.change(screen.getByLabelText(label), { target: { value: text } });
}

describe("tài khoản", () => {
  it("đăng ký: kiểm tra ô, chọn BTC, lỗi backend và chuyển sang email với đếm ngược", async () => {
    apiMock.onPost("/auth/resend-verification").reply(202);
    apiMock.onPost("/auth/register").replyOnce(409, { code: "EmailTaken" });
    apiMock.onPost("/auth/register").replyOnce(400, {
      code: "Validation",
      errors: { Organization: ["OrganizationRequired"] },
    });
    apiMock.onPost("/auth/register").reply(201, { email: "lan@example.com" });
    const user = userEvent.setup();
    const { memoryRouter } = await renderApp("/register");
    const form = within(screen.getByRole("form", { name: viText.auth.register.title }));
    await user.click(form.getByRole("button", { name: viText.auth.register.submit }));
    expect(await screen.findAllByText(viText.validation.NotEmptyValidator)).not.toHaveLength(0);
    expect(apiMock.history.post.filter((request) => request.url === "/auth/register")).toHaveLength(
      0,
    );
    await user.click(screen.getByRole("radio", { name: /Ban tổ chức/ }));
    expect(screen.getByLabelText(viText.auth.fields.organization)).toBeInTheDocument();
    expect(screen.getByText(viText.auth.register.approval)).toBeInTheDocument();
    fillField(viText.auth.fields.fullName, " A ");
    fillField(viText.auth.fields.email, "invalid");
    fillField(viText.auth.fields.password, "short");
    fillField(viText.auth.fields.confirmPassword, "different");
    await user.click(form.getByRole("button", { name: viText.auth.register.submit }));
    expect(await screen.findByText(viText.validation.fullName)).toBeInTheDocument();
    expect(screen.getByText(viText.validation.email)).toBeInTheDocument();
    expect(screen.getByText(viText.validation.confirm)).toBeInTheDocument();
    expect(screen.getByLabelText(viText.auth.fields.organization)).toHaveAttribute(
      "aria-invalid",
      "true",
    );
    fillField(viText.auth.fields.fullName, " Mai Lan ");
    fillField(viText.auth.fields.email, "lan@example.com");
    fillField(viText.auth.fields.organization, " ConfHub ");
    fillField(viText.auth.fields.password, "Password123");
    fillField(viText.auth.fields.confirmPassword, "Password123");
    await user.click(form.getByRole("button", { name: viText.auth.register.submit }));
    await waitFor(() => expect(form.getByText(viText.errors.EmailTaken)).toBeInTheDocument());
    await act(() => changeAppLanguage("en"));
    await waitFor(() => expect(form.getByText(enText.errors.EmailTaken)).toBeInTheDocument());
    await act(() => changeAppLanguage("vi"));
    await user.click(form.getByRole("button", { name: viText.auth.register.submit }));
    expect(await form.findByText(viText.errors.OrganizationRequired)).toBeInTheDocument();
    await user.click(form.getByRole("button", { name: viText.auth.register.submit }));
    expect(
      await screen.findByRole("heading", { name: viText.auth.checkEmail.title }),
    ).toBeInTheDocument();
    expect(memoryRouter.state.location.search).toBe("?email=lan%40example.com");
    expect(screen.getByRole("button", { name: /Gửi lại email xác thực/ })).toBeDisabled();
    expect(apiMock.history.post.filter((request) => request.url === "/auth/refresh")).toHaveLength(
      1,
    );
    // Interval đã được tạo khi trang mount; chỉ dời đồng hồ, để interval thật cập nhật giao diện.
    const dateNow = vi.spyOn(Date, "now").mockReturnValue(Date.now() + 61_000);
    await waitFor(
      () =>
        expect(screen.getByRole("button", { name: viText.auth.checkEmail.resend })).toBeEnabled(),
      { timeout: 2000 },
    );
    await user.click(screen.getByRole("button", { name: viText.auth.checkEmail.resend }));
    expect((await screen.findAllByText(viText.auth.checkEmail.resent)).length).toBeGreaterThan(0);
    expect(screen.getByRole("button", { name: /Gửi lại email xác thực/ })).toBeDisabled();
    dateNow.mockRestore();
  }, 60_000);

  it("xác thực: Active, chờ duyệt, token hỏng/thiếu; mỗi token chỉ gửi một lần trong StrictMode", async () => {
    apiMock.onPost("/auth/resend-verification").reply(202);
    apiMock.onPost("/auth/verify-email", { token: "active" }).reply(200, { status: "Active" });
    apiMock
      .onPost("/auth/verify-email", { token: "pending" })
      .reply(200, { status: "PendingApproval" });
    apiMock.onPost("/auth/verify-email", { token: "invalid" }).reply(422, { code: "TokenInvalid" });
    const { memoryRouter } = await renderApp("/verify-email?token=active");
    expect(await screen.findByText(viText.auth.verifyEmail.successDescription)).toBeInTheDocument();
    await act(() => memoryRouter.navigate("/verify-email?token=pending"));
    expect(await screen.findByText(viText.auth.verifyEmail.pendingDescription)).toBeInTheDocument();
    await act(() => memoryRouter.navigate("/verify-email?token=invalid"));
    expect(await screen.findByText(viText.auth.verifyEmail.invalidTitle)).toBeInTheDocument();
    await act(() => memoryRouter.navigate("/verify-email?token=active"));
    expect(await screen.findByText(viText.auth.verifyEmail.successDescription)).toBeInTheDocument();
    await act(() => memoryRouter.navigate("/verify-email"));
    expect(await screen.findByText(viText.auth.verifyEmail.invalidTitle)).toBeInTheDocument();
    expect(
      apiMock.history.post.filter((request) => request.url === "/auth/verify-email"),
    ).toHaveLength(3);
    fillField(viText.auth.fields.email, "lan@example.com");
    await userEvent.click(screen.getByRole("button", { name: viText.auth.checkEmail.resend }));
    expect((await screen.findAllByText(viText.auth.checkEmail.resent)).length).toBeGreaterThan(0);
    expect(screen.getByRole("button", { name: /Gửi lại email xác thực/ })).toBeDisabled();
  }, 60_000);

  it("đăng nhập: dịch lỗi 401/403, hiện tên; đăng xuất lỗi vẫn xóa phiên và cache", async () => {
    const codes = [
      "InvalidCredentials",
      "AccountUnverified",
      "AccountPending",
      "AccountLocked",
    ] as const;
    apiMock.onPost("/auth/logout").networkError();
    apiMock.onPost("/auth/resend-verification").reply(202);
    for (const code of codes) {
      apiMock.onPost("/auth/login").replyOnce(code === "InvalidCredentials" ? 401 : 403, { code });
    }
    apiMock.onPost("/auth/login").reply(200, session);
    const user = userEvent.setup();
    const { memoryRouter, queryClient } = await renderApp(
      "/login?reason=sessionExpired&returnUrl=//evil.com",
    );
    expect(screen.getByText(viText.errors.SessionExpired)).toBeInTheDocument();
    fillField(viText.auth.fields.email, "lan@example.com");
    fillField(viText.auth.fields.password, "Password123");
    const form = within(screen.getByRole("form", { name: viText.auth.login.title }));
    for (const code of codes) {
      await user.click(form.getByRole("button", { name: viText.auth.header.login }));
      expect(await screen.findByText(viText.errors[code])).toBeInTheDocument();
      if (code === "AccountUnverified") {
        // Nút gửi lại email xác thực tới đúng email đang nhập, thông báo chung (BR09).
        await user.click(screen.getByRole("button", { name: viText.auth.checkEmail.resend }));
        expect(await screen.findByText(viText.auth.checkEmail.resent)).toBeInTheDocument();
        expect(apiMock.history.post).toContainEqual(
          expect.objectContaining({
            url: "/auth/resend-verification",
            data: JSON.stringify({ email: "lan@example.com" }),
          }),
        );
      }
    }
    await user.click(form.getByRole("button", { name: viText.auth.header.login }));
    expect(await screen.findByText(session.user.fullName)).toBeInTheDocument();
    expect(memoryRouter.state.location.pathname).toBe("/");
    expect(window.localStorage.getItem("accessToken")).toBeNull();
    expect(window.sessionStorage.length).toBe(0);
    for (const path of ["/register", "/login", "/forgot-password"]) {
      await act(() => memoryRouter.navigate(path));
      await waitFor(() => expect(memoryRouter.state.location.pathname).toBe("/"));
    }
    queryClient.setQueryData(["private-data"], { secret: "cached" });
    await user.click(screen.getByRole("button", { name: /Tài khoản của Mai Lan/ }));
    expect(await screen.findByText(session.user.email)).toBeInTheDocument();
    expect(screen.getByText(viText.role.Organizer.name)).toBeInTheDocument();
    await user.click(screen.getByRole("menuitem", { name: viText.auth.userMenu.logout }));
    expect(
      await screen.findByRole("button", { name: viText.auth.header.login }),
    ).toBeInTheDocument();
    expect(getSession()).toBeNull();
    expect(queryClient.getQueryData(["private-data"])).toBeUndefined();
    expect(apiMock.history.post.filter((request) => request.url === "/auth/refresh")).toHaveLength(
      1,
    );
  }, 60_000);

  it("khôi phục cookie hiện user; đăng xuất thành công về khách", async () => {
    apiMock.onPost("/auth/refresh").reply(200, session);
    apiMock.onPost("/auth/logout").reply(204);
    await renderApp();
    expect(await screen.findByText(session.user.fullName)).toBeInTheDocument();
    await userEvent.click(screen.getByRole("button", { name: /Tài khoản của Mai Lan/ }));
    await userEvent.click(screen.getByRole("menuitem", { name: viText.auth.userMenu.logout }));
    expect(
      await screen.findByRole("button", { name: viText.auth.header.login }),
    ).toBeInTheDocument();
    expect(apiMock.history.post.map((request) => request.url)).toEqual([
      "/auth/refresh",
      "/auth/logout",
    ]);
    expect(getSession()).toBeNull();
  }, 30_000);

  it("quên mật khẩu luôn thông báo chung; đặt lại báo token hỏng hoặc thành công", async () => {
    apiMock.onPost("/auth/forgot-password").reply(202);
    apiMock
      .onPost("/auth/reset-password", {
        token: "invalid",
        newPassword: "NewPassword123",
      })
      .reply(422, { code: "TokenInvalid" });
    apiMock
      .onPost("/auth/reset-password", {
        token: "valid",
        newPassword: "NewPassword123",
      })
      .reply(204);
    const user = userEvent.setup();
    const { memoryRouter } = await renderApp("/forgot-password");
    fillField(viText.auth.fields.email, "nobody@example.com");
    await user.click(screen.getByRole("button", { name: viText.auth.forgotPassword.submit }));
    expect(await screen.findByText(viText.auth.forgotPassword.sent)).toBeInTheDocument();
    await act(() => memoryRouter.navigate("/reset-password"));
    expect(await screen.findByText(viText.auth.verifyEmail.invalidTitle)).toBeInTheDocument();
    for (const rawToken of ["invalid", "valid"]) {
      await act(() => memoryRouter.navigate(`/reset-password?token=${rawToken}`));
      fillField(viText.auth.fields.newPassword, "NewPassword123");
      fillField(viText.auth.fields.confirmPassword, "NewPassword123");
      await user.click(screen.getByRole("button", { name: viText.auth.resetPassword.submit }));
      expect(
        await screen.findByText(
          rawToken === "invalid"
            ? viText.auth.verifyEmail.invalidTitle
            : viText.auth.resetPassword.successTitle,
        ),
      ).toBeInTheDocument();
    }
    const resetCalls = apiMock.history.post.filter(
      (request) => request.url === "/auth/reset-password",
    );
    expect(resetCalls).toHaveLength(2);
    expect(JSON.parse(String(resetCalls[1].data))).toEqual({
      token: "valid",
      newPassword: "NewPassword123",
    });
  }, 60_000);
});
