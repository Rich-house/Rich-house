import { HttpErrorResponse } from '@angular/common/http';

export type ApiErrorKind = 'unavailable' | 'unauthorized' | 'server' | 'unknown';

export interface ApiErrorDetails {
  kind: ApiErrorKind;
  message: string;
  status: number | null;
  title: string;
}

interface ApiErrorMessages {
  unavailable?: string;
  unauthorized?: string;
  server?: string;
  unknown?: string;
}

const defaultMessages: Record<ApiErrorKind, { title: string; message: string }> = {
  unavailable: {
    title: 'Service temporarily unavailable',
    message:
      'The Rich House service is temporarily unreachable. Please wait a moment and try again.',
  },
  unauthorized: {
    title: 'Sign in required',
    message: 'Your session is no longer valid. Please sign in again.',
  },
  server: {
    title: 'Server error',
    message: 'The server returned an unexpected error. Please try again in a moment.',
  },
  unknown: {
    title: 'Request failed',
    message: 'We could not complete this request right now. Please try again.',
  },
};

export const describeApiError = (
  error: unknown,
  messages: ApiErrorMessages = {},
): ApiErrorDetails => {
  if (!(error instanceof HttpErrorResponse)) {
    return {
      kind: 'unknown',
      message: messages.unknown || defaultMessages.unknown.message,
      status: null,
      title: defaultMessages.unknown.title,
    };
  }

  if (error.status === 0) {
    return {
      kind: 'unavailable',
      message: messages.unavailable || defaultMessages.unavailable.message,
      status: 0,
      title: defaultMessages.unavailable.title,
    };
  }

  if (error.status === 401 || error.status === 403) {
    return {
      kind: 'unauthorized',
      message: messages.unauthorized || defaultMessages.unauthorized.message,
      status: error.status,
      title: defaultMessages.unauthorized.title,
    };
  }

  if (error.status >= 500) {
    return {
      kind: 'server',
      message: messages.server || defaultMessages.server.message,
      status: error.status,
      title: defaultMessages.server.title,
    };
  }

  return {
    kind: 'unknown',
    message: messages.unknown || defaultMessages.unknown.message,
    status: error.status,
    title: defaultMessages.unknown.title,
  };
};
