import React from 'react';

export const TableSkeleton: React.FC<{ rows?: number }> = ({ rows = 5 }) => {
  return (
    <div className="animate-pulse divide-y divide-slate-100">
      {Array.from({ length: rows }).map((_, i) => (
        <div key={i} className="py-4 px-6 flex items-center justify-between gap-4">
          <div className="h-5 bg-slate-200 rounded w-1/3"></div>
          <div className="flex gap-6 w-1/3 justify-center">
            <div className="h-5 bg-slate-200 rounded w-12"></div>
            <div className="h-5 bg-slate-200 rounded w-12"></div>
            <div className="h-5 bg-slate-200 rounded w-12"></div>
          </div>
          <div className="h-8 bg-slate-200 rounded-lg w-28"></div>
        </div>
      ))}
    </div>
  );
};

export const CardSkeleton: React.FC<{ cards?: number }> = ({ cards = 3 }) => {
  return (
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 animate-pulse">
      {Array.from({ length: cards }).map((_, i) => (
        <div key={i} className="bg-white p-5 rounded-2xl border border-slate-200 shadow-sm">
          <div className="h-6 bg-slate-200 rounded w-3/4 mb-4"></div>
          <div className="space-y-2 mb-6">
            <div className="h-4 bg-slate-100 rounded w-full"></div>
            <div className="h-4 bg-slate-100 rounded w-5/6"></div>
          </div>
          <div className="h-10 bg-slate-200 rounded-xl w-full"></div>
        </div>
      ))}
    </div>
  );
};
