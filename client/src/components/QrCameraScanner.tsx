import { useEffect, useId, useRef, useState } from "react";
import { Html5Qrcode, type CameraDevice } from "html5-qrcode";

type QrCameraScannerProps = {
  onDetected: (value: string) => void;
};

function toReadableErrorMessage(reason: unknown) {
  const rawMessage = typeof reason === "string"
    ? reason
    : reason instanceof Error
      ? reason.message
      : "";

  if (/NotAllowedError|Permission denied|permission/i.test(rawMessage)) {
    return "Camera access was blocked. Change this site's Camera permission from Ask to Allow, then refresh and try again.";
  }

  if (/NotFoundError|Requested device not found|device not found/i.test(rawMessage)) {
    return "No camera was found on this device.";
  }

  if (/NotReadableError|TrackStartError|could not start video source|device in use/i.test(rawMessage)) {
    return "The camera is busy in another app or browser tab. Close the other app and try again.";
  }

  return "Camera access failed. Set this site's Camera permission to Allow and try again.";
}

function QrCameraScanner({ onDetected }: QrCameraScannerProps) {
  const scannerId = useId().replace(/:/g, "");
  const [error, setError] = useState("");
  const onDetectedRef = useRef(onDetected);

  useEffect(() => {
    onDetectedRef.current = onDetected;
  }, [onDetected]);

  useEffect(() => {
    let isMounted = true;
    let hasDetectedCode = false;
    let scanner: Html5Qrcode | null = null;

    const clearScanner = (scannerToClear: Html5Qrcode | null = scanner) => {
      if (!scannerToClear) {
        return;
      }

      try {
        scannerToClear.clear();
      } catch {
        // Ignore cleanup errors from partially initialized scanner instances.
      }
    };

    const handleDecoded = (decodedText: string) => {
      if (!isMounted || hasDetectedCode) {
        return;
      }

      hasDetectedCode = true;
      onDetectedRef.current(decodedText.trim());
    };

    const scanConfig = { fps: 10, qrbox: { width: 260, height: 260 } };
    const isMobileDevice = /android|iphone|ipad|ipod|mobile/i.test(navigator.userAgent);

    const createScanner = () => {
      try {
        scanner = new Html5Qrcode(scannerId);
        return scanner;
      } catch {
        if (isMounted) {
          setError("The QR scanner could not be created in this browser.");
        }
        return null;
      }
    };

    const stopScannerIfRunning = async (scannerToStop: Html5Qrcode | null = scanner) => {
      if (!scannerToStop) {
        return;
      }

      try {
        await scannerToStop.stop();
      } catch {
        // The scanner may never have reached the running state, which is okay.
      } finally {
        clearScanner(scannerToStop);
      }
    };

    const wait = (milliseconds: number) => new Promise((resolve) => {
      window.setTimeout(resolve, milliseconds);
    });

    const startWithCamera = async (cameraIdOrConfig: string | MediaTrackConstraints) => {
      const nextScanner = createScanner();

      if (!nextScanner) {
        throw new Error("The QR scanner could not be created in this browser.");
      }

      try {
        await nextScanner.start(
          cameraIdOrConfig,
          scanConfig,
          handleDecoded,
          () => {
          },
        );
      } catch (startError) {
        await stopScannerIfRunning(nextScanner);
        scanner = null;
        throw startError;
      }
    };

    const startScanner = async () => {
      const facingModeCandidates: MediaTrackConstraints[] = isMobileDevice
        ? [{ facingMode: "environment" }, { facingMode: "user" }]
        : [{ facingMode: "user" }, { facingMode: "environment" }];

      // Try the simple browser camera modes first before falling back to a device id.
      for (const candidate of facingModeCandidates) {
        try {
          await startWithCamera(candidate);
          return;
        } catch {
          await stopScannerIfRunning();
          scanner = null;
        }
      }

      try {
        const cameras = await Html5Qrcode.getCameras();
        const fallbackCamera = cameras.find((camera: CameraDevice) => /back|rear|environment/i.test(camera.label))
          ?? cameras[0];

        if (!fallbackCamera) {
          throw new Error("No camera was found on this device.");
        }

        // The library briefly opens and closes a stream while listing cameras.
        // Waiting a moment avoids "camera busy" failures on some Windows devices.
        await wait(250);
        await startWithCamera(fallbackCamera.id);
      } catch (fallbackError) {
        if (isMounted) {
          setError(toReadableErrorMessage(fallbackError));
        }
      }
    };

    // We start the camera as soon as the dialog opens so the librarian can scan in one step.
    void startScanner();

    return () => {
      isMounted = false;

      void stopScannerIfRunning();
    };
  }, [scannerId]);

  return (
    <div className="space-y-3">
      <div id={scannerId} className="overflow-hidden rounded-3xl bg-black" />
      {error && (
        <p className="rounded-2xl bg-rose-50 px-4 py-3 text-sm text-rose-700 ring-1 ring-rose-200">
          {error}
        </p>
      )}
      <p className="text-sm text-slate-500">
        Point the browser camera at the member QR code. The book will be issued as soon as a valid member code is detected.
      </p>
    </div>
  );
}

export default QrCameraScanner;
