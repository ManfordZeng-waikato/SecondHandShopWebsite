/* eslint-disable react-hooks/set-state-in-effect */
import { Alert, Box } from '@mui/material';
import { forwardRef, useEffect, useImperativeHandle, useRef, useState } from 'react';

const turnstileScriptId = 'cloudflare-turnstile-script';
const turnstileScriptUrl = 'https://challenges.cloudflare.com/turnstile/v0/api.js?render=explicit';
let turnstileScriptLoadingPromise: Promise<void> | null = null;

interface TurnstileRenderOptions {
  sitekey: string;
  callback?: (token: string) => void;
  'expired-callback'?: () => void;
  'error-callback'?: () => void;
  theme?: 'light' | 'dark' | 'auto';
  language?: string;
  execution?: 'render' | 'execute';
}

interface TurnstileApi {
  render(container: HTMLElement, options: TurnstileRenderOptions): string;
  execute(widgetId?: string): void;
  reset(widgetId?: string): void;
  remove(widgetId?: string): void;
}

declare global {
  interface Window {
    turnstile?: TurnstileApi;
  }
}

export interface TurnstileWidgetProps {
  siteKey: string;
  resetKey: number;
  language?: string;
  executionMode?: 'render' | 'execute';
  onVerify: (token: string) => void;
  onExpire: () => void;
  onError: () => void;
}

export interface TurnstileWidgetHandle {
  execute: () => boolean;
  reset: () => void;
}

function loadTurnstileScript(): Promise<void> {
  if (window.turnstile) {
    return Promise.resolve();
  }

  if (turnstileScriptLoadingPromise) {
    return turnstileScriptLoadingPromise;
  }

  turnstileScriptLoadingPromise = new Promise<void>((resolve, reject) => {
    const existingScript = document.getElementById(turnstileScriptId) as HTMLScriptElement | null;
    if (existingScript) {
      if (window.turnstile) {
        resolve();
        return;
      }

      existingScript.addEventListener('load', () => resolve(), { once: true });
      existingScript.addEventListener('error', () => {
        turnstileScriptLoadingPromise = null;
        reject(new Error('Failed to load Cloudflare Turnstile script.'));
      }, { once: true });
      return;
    }

    const script = document.createElement('script');
    script.id = turnstileScriptId;
    script.src = turnstileScriptUrl;
    script.async = true;
    script.defer = true;
    script.addEventListener('load', () => resolve(), { once: true });
    script.addEventListener('error', () => {
      turnstileScriptLoadingPromise = null;
      reject(new Error('Failed to load Cloudflare Turnstile script.'));
    }, { once: true });
    document.head.append(script);
  });

  return turnstileScriptLoadingPromise;
}

export const TurnstileWidget = forwardRef<TurnstileWidgetHandle, TurnstileWidgetProps>(function TurnstileWidget(
  {
    siteKey,
    resetKey,
    language = 'en',
    executionMode = 'execute',
    onVerify,
    onExpire,
    onError,
  }: TurnstileWidgetProps,
  ref,
) {
  const containerRef = useRef<HTMLDivElement | null>(null);
  const widgetIdRef = useRef<string | null>(null);
  const [isScriptReady, setIsScriptReady] = useState(false);
  const [loadError, setLoadError] = useState<string | null>(null);

  useImperativeHandle(ref, () => ({
    execute: () => {
      if (!widgetIdRef.current || !window.turnstile) {
        return false;
      }

      window.turnstile.execute(widgetIdRef.current);
      return true;
    },
    reset: () => {
      if (!widgetIdRef.current || !window.turnstile) {
        return;
      }

      window.turnstile.reset(widgetIdRef.current);
    },
  }), []);

  useEffect(() => {
    if (!siteKey.trim()) {
      setLoadError('Security verification is not configured. Please contact support.');
      return;
    }

    let isMounted = true;
    loadTurnstileScript()
      .then(() => {
        if (!isMounted) {
          return;
        }

        setLoadError(null);
        setIsScriptReady(true);
      })
      .catch(() => {
        if (!isMounted) {
          return;
        }

        setLoadError('Unable to load security verification. Please refresh and try again.');
        onError();
      });

    return () => {
      isMounted = false;
    };
  }, [onError, siteKey]);

  useEffect(() => {
    if (!isScriptReady || !containerRef.current || !window.turnstile || widgetIdRef.current) {
      return;
    }

    try {
      widgetIdRef.current = window.turnstile.render(containerRef.current, {
        sitekey: siteKey,
        theme: 'auto',
        language,
        execution: executionMode,
        callback: (token) => onVerify(token),
        'expired-callback': () => onExpire(),
        'error-callback': () => onError(),
      });
    } catch {
      setLoadError('Unable to initialize security verification. Please refresh and try again.');
      onError();
    }

    return () => {
      if (!widgetIdRef.current || !window.turnstile) {
        return;
      }

      window.turnstile.remove(widgetIdRef.current);
      widgetIdRef.current = null;
    };
  }, [executionMode, isScriptReady, language, onError, onExpire, onVerify, siteKey]);

  useEffect(() => {
    if (!widgetIdRef.current || !window.turnstile) {
      return;
    }

    window.turnstile.reset(widgetIdRef.current);
  }, [resetKey]);

  return (
    <Box>
      {loadError ? (
        <Alert severity="error">{loadError}</Alert>
      ) : (
        <Box ref={containerRef} sx={{ minHeight: 65 }} />
      )}
    </Box>
  );
});
