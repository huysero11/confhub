// apiMock.ts — bộ giả phản hồi axios thuộc lớp hỗ trợ test, không gọi backend thật.
// setup và các test dùng chung adapter gắn vào apiClient để khai phản hồi, kiểm lịch sử gọi.
import MockAdapter from "axios-mock-adapter";
import { apiClient } from "@/lib/apiClient";

export const apiMock = new MockAdapter(apiClient);
