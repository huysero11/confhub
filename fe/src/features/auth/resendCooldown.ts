// resendCooldown.ts — thời gian chờ giữa hai lần gửi lại email (nhóm Tài khoản).
// Chứa: hằng RESEND_COOLDOWN_MS.
// BR08: phải khớp TokenLifetimes.ResendCooldown của backend (60 giây).
export const RESEND_COOLDOWN_MS = 60_000;
