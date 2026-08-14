"use client";

import { motion } from "motion/react";

export interface DonutSegment {
  label: string;
  value: number;
  color: string;
}

export function Donut({
  segments,
  size = 176,
  thickness = 18,
}: {
  segments: DonutSegment[];
  size?: number;
  thickness?: number;
}) {
  const total = segments.reduce((s, x) => s + x.value, 0);
  const radius = (size - thickness) / 2;
  const circ = 2 * Math.PI * radius;

  let cumulative = 0;

  return (
    <div className="relative" style={{ width: size, height: size }}>
      <svg width={size} height={size} className="-rotate-90">
        <circle
          cx={size / 2}
          cy={size / 2}
          r={radius}
          fill="none"
          stroke="rgba(255,255,255,0.05)"
          strokeWidth={thickness}
        />
        {total > 0 &&
          segments.map((seg, i) => {
            const frac = seg.value / total;
            const dash = frac * circ;
            const offset = cumulative;
            cumulative += dash;
            return (
              <motion.circle
                key={seg.label}
                cx={size / 2}
                cy={size / 2}
                r={radius}
                fill="none"
                stroke={seg.color}
                strokeWidth={thickness}
                strokeLinecap="round"
                strokeDashoffset={-offset}
                initial={{ strokeDasharray: `0 ${circ}` }}
                animate={{ strokeDasharray: `${Math.max(dash - 2, 0)} ${circ}` }}
                transition={{
                  duration: 0.9,
                  ease: [0.22, 1, 0.36, 1],
                  delay: 0.08 * i,
                }}
              />
            );
          })}
      </svg>
      <div className="absolute inset-0 flex flex-col items-center justify-center">
        <span className="text-2xl font-semibold text-white">{total}</span>
        <span className="text-xs text-slate-400">items</span>
      </div>
    </div>
  );
}
