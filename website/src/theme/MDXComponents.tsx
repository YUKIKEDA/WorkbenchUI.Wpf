import React, {type ComponentProps} from 'react';
import MDXComponents from '@theme-original/MDXComponents';

const HEX_COLOR_PATTERN = /^#([0-9A-Fa-f]{6}|[0-9A-Fa-f]{8})$/;

function toSwatchBackground(hex: string): string {
  const digits = hex.slice(1);
  if (digits.length === 6) {
    return hex;
  }

  // WPF 8-digit hex is #AARRGGBB (alpha first, unlike CSS #RRGGBBAA).
  const a = parseInt(digits.slice(0, 2), 16) / 255;
  const r = parseInt(digits.slice(2, 4), 16);
  const g = parseInt(digits.slice(4, 6), 16);
  const b = parseInt(digits.slice(6, 8), 16);
  const rgba = `rgba(${r}, ${g}, ${b}, ${a.toFixed(3)})`;

  if (r === 255 && g === 255 && b === 255) {
    return `linear-gradient(${rgba}, ${rgba}), #252526`;
  }
  if (r === 0 && g === 0 && b === 0) {
    return `linear-gradient(${rgba}, ${rgba}), #F5F5F7`;
  }
  return `linear-gradient(${rgba}, ${rgba}), repeating-conic-gradient(#CCCCCC 0% 25%, #FFFFFF 0% 50%) 50% / 8px 8px`;
}

const DefaultCode = MDXComponents.code;

function CodeWithColorSwatch(props: ComponentProps<'code'>) {
  if (!props.className && typeof props.children === 'string') {
    const trimmed = props.children.trim();
    if (HEX_COLOR_PATTERN.test(trimmed)) {
      const background = toSwatchBackground(trimmed);
      return (
        <span className="wb-color-token">
          <span className="wb-color-swatch" style={{background}} aria-hidden="true" />
          <DefaultCode {...props} />
        </span>
      );
    }
  }

  return <DefaultCode {...props} />;
}

export default {
  ...MDXComponents,
  code: CodeWithColorSwatch,
};
