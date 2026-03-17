export type NoticeTone = "success" | "error" | "info";

export type Notice = {
  id: number;
  tone: NoticeTone;
  message: string;
};

type Listener = (notice: Notice) => void;

const listeners = new Set<Listener>();
let nextNoticeId = 1;

export function subscribeToNotices(listener: Listener) {
  listeners.add(listener);

  return () => {
    listeners.delete(listener);
  };
}

export function notify(message: string, tone: NoticeTone = "info") {
  const notice: Notice = {
    id: nextNoticeId++,
    tone,
    message,
  };

  listeners.forEach((listener) => listener(notice));
}

export function notifySuccess(message: string) {
  notify(message, "success");
}

export function notifyError(message: string) {
  notify(message, "error");
}

export function extractApiMessage(error: unknown, fallbackMessage: string) {
  if (typeof error === "object" && error && "response" in error) {
    const response = (error as { response?: { data?: { message?: string } | string } }).response;
    const responseData = response?.data;

    if (typeof responseData === "string" && responseData.trim().length > 0) {
      return responseData;
    }

    if (typeof responseData === "object" && responseData && "message" in responseData) {
      const message = (responseData as { message?: string }).message;
      if (message && message.trim().length > 0) {
        return message;
      }
    }
  }

  return fallbackMessage;
}
