// apiClient.test.ts — test lớp lib bằng axios-mock-adapter, không cần backend đang chạy.
// Kiểm hợp đồng lỗi, token, gửi lại một lần và các cuộc gọi refresh trùng nhau.
import { describe, expect, it, vi } from "vitest";
import { apiClient, refreshSession } from "./apiClient";
import { ApiError, isApiError } from "./apiError";
import { clearSession, getSession, setSession } from "./authSession";
import type { SessionResponse } from "@/lib/authSession";
import { resolvePostLoginPath } from "@/features/auth/roleLandingPaths";
import { forgotPassword, logout } from "@/features/auth/api";
import { apiMock } from "@/test/apiMock";

const session: SessionResponse = {
  accessToken: "access-old",
  expiresIn: 900,
  user: {
    id: "user-1",
    fullName: "Mai Lan",
    email: "lan@example.com",
    role: "Organizer",
    organization: "ConfHub",
    permissions: [],
  },
};

describe("apiClient và điều hướng", () => {
  it("đổi ProblemDetails, lỗi mạng/body và xử lý response rỗng", async () => {
    apiMock.onPost("/auth/register").reply(400, {
      code: "Validation",
      errors: {
        Email: ["EmailValidator"],
        FullName: ["FullNameLength"],
        Organization: "invalid",
        Password: ["required", 123],
      },
    });
    apiMock.onGet("/example").replyOnce(502, "bad gateway");
    apiMock.onGet("/example").networkErrorOnce();
    apiMock.onPost("/auth/forgot-password").reply(202);
    apiMock.onPost("/auth/logout").reply(204);
    apiMock.onGet("/example").replyOnce(500, {});
    await expect(apiClient.post("/auth/register", { email: "bad" })).rejects.toMatchObject({
      status: 400,
      code: "Validation",
      fieldErrors: { email: ["EmailValidator"], fullName: ["FullNameLength"] },
    });
    await expect(apiClient.get("/example")).rejects.toMatchObject({ code: "Network", status: 502 });
    await expect(apiClient.get("/example")).rejects.toMatchObject({ code: "Network", status: 0 });
    await expect(forgotPassword("lan@example.com")).resolves.toBeUndefined();
    await expect(logout()).resolves.toBeUndefined();
    await expect(apiClient.get("/example")).rejects.toMatchObject({ code: "Unknown", status: 500 });
    expect(apiMock.history.post[0]).toMatchObject({
      baseURL: "/api",
      method: "post",
      data: JSON.stringify({ email: "bad" }),
    });
    expect(apiMock.history.post[0].withCredentials).toBeUndefined();
    expect(isApiError(new ApiError(400, "Validation"))).toBe(true);
    expect(isApiError(new Error())).toBe(false);
  });

  it("body rỗng, mảng hoặc giá trị đơn không được coi là ProblemDetails", async () => {
    for (const payload of [undefined, null, "", [], 123, true]) {
      apiMock.onGet("/example").replyOnce(502, payload);
      await expect(apiClient.get("/example")).rejects.toMatchObject({
        code: "Network",
        status: 502,
      });
    }
    apiMock.onGet("/example").replyOnce(500, { code: 123, errors: ["invalid"] });
    await expect(apiClient.get("/example")).rejects.toMatchObject({
      code: "Unknown",
      status: 500,
      fieldErrors: {},
    });
  });

  it("ném lại lỗi không phải AxiosError", async () => {
    const failure = new Error("interceptor failed");
    const interceptorId = apiClient.interceptors.request.use(() => {
      throw failure;
    });
    try {
      await expect(apiClient.get("/example")).rejects.toBe(failure);
      expect(apiMock.history.get).toHaveLength(0);
    } finally {
      apiClient.interceptors.request.eject(interceptorId);
    }
  });

  it("chia sẻ đúng một Promise refresh và không phục hồi phiên sau khi đã xóa", async () => {
    let completeRefresh!: (response: [number, SessionResponse]) => void;
    apiMock.onPost("/auth/refresh").reply(
      () =>
        new Promise<[number, SessionResponse]>((resolve) => {
          completeRefresh = resolve;
        }),
    );
    const firstRefresh = refreshSession();
    const secondRefresh = refreshSession();
    expect(firstRefresh).toBe(secondRefresh);
    await vi.waitFor(() => expect(apiMock.history.post).toHaveLength(1));
    completeRefresh([200, session]);
    await firstRefresh;
    expect(getSession()).toEqual(session);

    const lateRefresh = refreshSession();
    await vi.waitFor(() => expect(apiMock.history.post).toHaveLength(2));
    clearSession();
    completeRefresh([200, session]);
    await expect(lateRefresh).rejects.toMatchObject({ code: "SessionExpired" });
    expect(getSession()).toBeNull();
  });

  it("401 làm mới một lần rồi gửi lại cùng body với token mới", async () => {
    setSession(session);
    apiMock.onPost("/conferences").replyOnce(401, { code: "Unauthorized" });
    apiMock.onPost("/auth/refresh").reply(200, { ...session, accessToken: "access-new" });
    apiMock.onPost("/conferences").reply(200, { title: "protected" });
    await expect(apiClient.post("/conferences", { title: "conference" })).resolves.toMatchObject({
      data: { title: "protected" },
    });
    expect(apiMock.history.post.map((request) => request.url)).toEqual([
      "/conferences",
      "/auth/refresh",
      "/conferences",
    ]);
    expect(apiMock.history.post[0].headers?.Authorization).toBe("Bearer access-old");
    expect(apiMock.history.post[2].headers?.Authorization).toBe("Bearer access-new");
    expect(apiMock.history.post[2].data).toBe(apiMock.history.post[0].data);
    expect(apiMock.history.post[2]).toMatchObject({ isRetryAfterRefresh: true });
  });

  it("đường auth và request không gửi token không làm mới phiên", async () => {
    setSession(session);
    apiMock.onPost("/auth/login").reply(401, { code: "InvalidCredentials" });
    await expect(apiClient.post("/auth/login")).rejects.toMatchObject({
      code: "InvalidCredentials",
    });
    clearSession();
    apiMock.onGet("/conferences").reply(() => {
      // Phiên xuất hiện trong lúc chờ cũng không biến request khách thành request có token.
      setSession(session);
      return [401, { code: "Unauthorized" }];
    });
    await expect(apiClient.get("/conferences")).rejects.toMatchObject({ code: "Unauthorized" });
    expect(apiMock.history.get[0].headers?.Authorization).toBeUndefined();
    expect(apiMock.history.post).toHaveLength(1);
  });

  it("nhiều lỗi 401 đang chờ dùng chung một request refresh", async () => {
    setSession(session);
    let completeRefresh!: (response: [number, SessionResponse]) => void;
    apiMock.onPost("/auth/refresh").reply(
      () =>
        new Promise<[number, SessionResponse]>((resolve) => {
          completeRefresh = resolve;
        }),
    );
    apiMock.onGet("/conferences").replyOnce(401, { code: "Unauthorized" });
    apiMock.onGet("/conferences").replyOnce(401, { code: "Unauthorized" });
    apiMock.onGet("/conferences").reply(200, { title: "protected" });
    const firstRequest = apiClient.get("/conferences");
    const secondRequest = apiClient.get("/conferences");
    await vi.waitFor(() => expect(apiMock.history.post).toHaveLength(1));
    completeRefresh([200, { ...session, accessToken: "access-new" }]);
    await Promise.all([firstRequest, secondRequest]);
    expect(apiMock.history.post).toHaveLength(1);
    expect(apiMock.history.get).toHaveLength(4);
    expect(apiMock.history.get[2].headers?.Authorization).toBe("Bearer access-new");
    expect(apiMock.history.get[3].headers?.Authorization).toBe("Bearer access-new");
  });

  it("401 đến muộn dùng token đã làm mới, không xoay cookie thêm lần nữa", async () => {
    setSession(session);
    let completeLateRequest!: (response: [number, { code: string }]) => void;
    apiMock.onGet("/conferences").replyOnce(
      () =>
        new Promise<[number, { code: string }]>((resolve) => {
          completeLateRequest = resolve;
        }),
    );
    apiMock.onGet("/conferences").reply(200, { title: "protected" });
    apiMock.onPost("/auth/refresh").reply(200, { ...session, accessToken: "access-new" });
    const lateRequest = apiClient.get("/conferences");
    await vi.waitFor(() => expect(apiMock.history.get).toHaveLength(1));
    await refreshSession();
    completeLateRequest([401, { code: "Unauthorized" }]);
    await expect(lateRequest).resolves.toMatchObject({ data: { title: "protected" } });
    expect(apiMock.history.post).toHaveLength(1);
    expect(apiMock.history.get[1].headers?.Authorization).toBe("Bearer access-new");
  });

  it("401 đến sau khi xóa phiên chuyển tới đăng nhập, không gửi lại token cũ", async () => {
    const assignLocation = vi.fn();
    vi.stubGlobal("window", { location: { assign: assignLocation } });
    setSession(session);
    apiMock.onGet("/conferences").reply(() => {
      clearSession();
      return [401, { code: "Unauthorized" }];
    });
    await expect(apiClient.get("/conferences")).rejects.toMatchObject({ code: "SessionExpired" });
    expect(apiMock.history.get).toHaveLength(1);
    expect(apiMock.history.post).toHaveLength(0);
    expect(assignLocation).toHaveBeenCalledWith("/login?reason=sessionExpired");
  });

  it("refresh thất bại hoặc gửi lại vẫn 401 xóa phiên và chuyển tới đăng nhập", async () => {
    const assignLocation = vi.fn();
    vi.stubGlobal("window", { location: { assign: assignLocation } });
    for (const failsDuringRefresh of [true, false]) {
      apiMock.reset();
      setSession(session);
      apiMock.onGet("/conferences").reply(401, { code: "Unauthorized" });
      if (failsDuringRefresh) {
        apiMock.onPost("/auth/refresh").reply(401, { code: "SessionExpired" });
      } else {
        apiMock.onPost("/auth/refresh").reply(200, { ...session, accessToken: "access-new" });
      }
      await expect(apiClient.get("/conferences")).rejects.toMatchObject({
        code: "SessionExpired",
        status: 401,
      });
      expect(getSession()).toBeNull();
      expect(apiMock.history.post).toHaveLength(1);
      expect(apiMock.history.get).toHaveLength(failsDuringRefresh ? 1 : 2);
    }
    expect(assignLocation).toHaveBeenCalledTimes(2);
    expect(assignLocation).toHaveBeenCalledWith("/login?reason=sessionExpired");
  });

  it("lỗi khác 401 khi gửi lại được giữ nguyên và không xóa phiên", async () => {
    setSession(session);
    apiMock.onGet("/conferences").replyOnce(401, { code: "Unauthorized" });
    apiMock.onPost("/auth/refresh").reply(200, { ...session, accessToken: "access-new" });
    apiMock.onGet("/conferences").reply(403, { code: "Forbidden" });
    await expect(apiClient.get("/conferences")).rejects.toMatchObject({
      code: "Forbidden",
      status: 403,
    });
    expect(getSession()?.accessToken).toBe("access-new");
    expect(apiMock.history.post).toHaveLength(1);
    expect(apiMock.history.get).toHaveLength(2);
  });

  it("chỉ nhận returnUrl nội bộ, mọi vai trò hiện về trang chủ", () => {
    expect(resolvePostLoginPath(session.user, "/conferences?id=1#schedule")).toBe(
      "/conferences?id=1#schedule",
    );
    for (const returnUrl of [
      null,
      "//evil.com",
      "https://evil.com",
      "/\\evil.com",
      "/\n/evil.com",
    ]) {
      expect(resolvePostLoginPath(session.user, returnUrl)).toBe("/");
    }
    for (const role of ["Attendee", "Organizer", "Supplier", "Staff", "Admin"] as const) {
      expect(resolvePostLoginPath({ ...session.user, role }, null)).toBe("/");
    }
  });
});
