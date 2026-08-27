import {
  useEffect,
  useRef,
  useState
} from "react";

const canvasSize = 600;

function ProfilePhotoCropper({
  file,
  onApply,
  onCancel
}) {
  const canvasRef = useRef(null);
  const imageRef = useRef(null);
  const objectUrlRef = useRef(null);
  const dragRef = useRef(null);

  const [zoom, setZoom] = useState(1);
  const [position, setPosition] = useState({
    x: 0,
    y: 0
  });

  const [isDragging, setIsDragging] =
    useState(false);

  useEffect(() => {
    const image = new Image();
    const objectUrl = URL.createObjectURL(file);

    objectUrlRef.current = objectUrl;

    image.onload = () => {
      imageRef.current = image;
      setZoom(1);
      setPosition({ x: 0, y: 0 });
      drawImage(image, 1, { x: 0, y: 0 });
    };

    image.src = objectUrl;

    return () => {
      URL.revokeObjectURL(objectUrl);
    };
  }, [file]);

  useEffect(() => {
    if (imageRef.current) {
      drawImage(
        imageRef.current,
        zoom,
        position
      );
    }
  }, [zoom, position]);

  function getImageMeasurements(
    image,
    currentZoom
  ) {
    const baseScale = Math.max(
      canvasSize / image.naturalWidth,
      canvasSize / image.naturalHeight
    );

    const scale = baseScale * currentZoom;

    return {
      width: image.naturalWidth * scale,
      height: image.naturalHeight * scale
    };
  }

  function limitPosition(
    image,
    currentZoom,
    newPosition
  ) {
    const measurements =
      getImageMeasurements(image, currentZoom);

    const maximumX = Math.max(
      0,
      (measurements.width - canvasSize) / 2
    );

    const maximumY = Math.max(
      0,
      (measurements.height - canvasSize) / 2
    );

    return {
      x: Math.max(
        -maximumX,
        Math.min(maximumX, newPosition.x)
      ),
      y: Math.max(
        -maximumY,
        Math.min(maximumY, newPosition.y)
      )
    };
  }

  function drawImage(
    image,
    currentZoom,
    currentPosition
  ) {
    const canvas = canvasRef.current;

    if (!canvas) {
      return;
    }

    const context = canvas.getContext("2d");

    const measurements =
      getImageMeasurements(image, currentZoom);

    const safePosition = limitPosition(
      image,
      currentZoom,
      currentPosition
    );

    const imageX =
      (canvasSize - measurements.width) / 2 +
      safePosition.x;

    const imageY =
      (canvasSize - measurements.height) / 2 +
      safePosition.y;

    context.clearRect(
      0,
      0,
      canvasSize,
      canvasSize
    );

    context.fillStyle = "#ffffff";

    context.fillRect(
      0,
      0,
      canvasSize,
      canvasSize
    );

    context.drawImage(
      image,
      imageX,
      imageY,
      measurements.width,
      measurements.height
    );
  }

  function handlePointerDown(event) {
    event.currentTarget.setPointerCapture(
      event.pointerId
    );

    dragRef.current = {
      pointerX: event.clientX,
      pointerY: event.clientY,
      startingX: position.x,
      startingY: position.y
    };

    setIsDragging(true);
  }

  function handlePointerMove(event) {
    if (
      !dragRef.current ||
      !imageRef.current
    ) {
      return;
    }

    const canvas =
      canvasRef.current;

    const displayScale =
      canvasSize /
      canvas.getBoundingClientRect().width;

    const nextPosition = {
      x:
        dragRef.current.startingX +
        (event.clientX -
          dragRef.current.pointerX) *
          displayScale,

      y:
        dragRef.current.startingY +
        (event.clientY -
          dragRef.current.pointerY) *
          displayScale
    };

    setPosition(
      limitPosition(
        imageRef.current,
        zoom,
        nextPosition
      )
    );
  }

  function stopDragging() {
    dragRef.current = null;
    setIsDragging(false);
  }

  function handleWheel(event) {
    event.preventDefault();

    const direction =
      event.deltaY < 0 ? 0.1 : -0.1;

    setZoom((currentZoom) =>
      Math.max(
        0.4,
        Math.min(
          3,
          Number(
            (currentZoom + direction).toFixed(1)
          )
        )
      )
    );
  }

  function resetPhoto() {
    setZoom(1);
    setPosition({ x: 0, y: 0 });
  }

  function applyPhoto() {
    const canvas = canvasRef.current;

    canvas.toBlob(
      (blob) => {
        if (!blob) {
          return;
        }

        const fileName =
          file.name.replace(
            /\.[^/.]+$/,
            ""
          ) + "-profile.jpg";

        const adjustedFile = new File(
          [blob],
          fileName,
          {
            type: "image/jpeg",
            lastModified: Date.now()
          }
        );

        onApply(adjustedFile);
      },
      "image/jpeg",
      0.9
    );
  }

  return (
    <div className="photo-cropper">
      <div className="photo-cropper-heading">
        <div>
          <h3>Adjust profile picture</h3>

          <p>
            Drag the photo to reposition it.
            Use the mouse wheel to zoom.
          </p>
        </div>
      </div>

      <div className="photo-cropper-layout">
        <canvas
          ref={canvasRef}
          width={canvasSize}
          height={canvasSize}
          className={
            isDragging
              ? "photo-crop-canvas dragging"
              : "photo-crop-canvas"
          }
          onPointerDown={handlePointerDown}
          onPointerMove={handlePointerMove}
          onPointerUp={stopDragging}
          onPointerCancel={stopDragging}
          onWheel={handleWheel}
        />

        <div className="photo-cropper-controls">
          <span>
            Zoom: {Math.round(zoom * 100)}%
          </span>

          <button
            type="button"
            className="secondary-button"
            onClick={() =>
              setZoom((current) =>
                Math.max(
                  0.4,
                  Number((current - 0.1).toFixed(1))
                )
              )
            }
          >
            −
          </button>

          <button
            type="button"
            className="secondary-button"
            onClick={() =>
              setZoom((current) =>
                Math.min(3, current + 0.1)
              )
            }
          >
            +
          </button>
        </div>
      </div>

      <div className="photo-cropper-actions">
        <button
          type="button"
          className="secondary-button"
          onClick={onCancel}
        >
          Choose another
        </button>

        <button
          type="button"
          className="secondary-button"
          onClick={resetPhoto}
        >
          Reset
        </button>

        <button
          type="button"
          className="photo-choose-button"
          onClick={applyPhoto}
        >
          Apply photo
        </button>
      </div>
    </div>
  );
}

export default ProfilePhotoCropper;