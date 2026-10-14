// ResendCountdownButton.tsx — component nút đếm ngược dùng ở hai luồng gửi lại email.
// Thời điểm cho phép gửi do component cha cập nhật; tính theo đồng hồ để tab nền không làm lệch 60 giây.
import { useEffect, useState } from "react";
import { Button } from "antd";
import { useTranslation } from "react-i18next";

type ResendCountdownButtonProps = {
  availableAt: number;
  loading: boolean;
  disabled?: boolean;
  onClick?: () => void;
  htmlType?: "button" | "submit";
};

export function ResendCountdownButton({
  availableAt,
  loading,
  disabled = false,
  onClick,
  htmlType = "button",
}: ResendCountdownButtonProps) {
  const { t } = useTranslation();
  const [currentTime, setCurrentTime] = useState(Date.now);
  const remainingSeconds = Math.max(0, Math.ceil((availableAt - currentTime) / 1000));

  // Cập nhật đồng hồ đếm ngược
  useEffect(() => {
    if (remainingSeconds === 0) {
      return;
    }

    const timer = window.setInterval(() => {
      setCurrentTime(Date.now());
    }, 1000);

    return () => {
      window.clearInterval(timer);
    };
  }, [remainingSeconds]);

  return (
    <Button
      block
      htmlType={htmlType}
      onClick={onClick}
      loading={loading}
      disabled={disabled || loading || remainingSeconds > 0}
    >
      {remainingSeconds > 0
        ? t("auth.resendCountdown", { seconds: remainingSeconds })
        : t("auth.checkEmail.resend")}
    </Button>
  );
}
