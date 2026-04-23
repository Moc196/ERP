import React, { useEffect, useRef, useState } from 'react';
import api from '../api/axios';
import { Bell, X, CheckCheck, AlertTriangle, Clock, TrendingDown, PackageX, Activity } from 'lucide-react';
import { useAuth } from '../context/AuthContext';

interface Alert {
  id: number;
  type: string;
  severity: string;
  title: string;
  message: string;
  isRead: boolean;
  createdAt: string;
}

const typeIcon = (type: string) => {
  switch (type) {
    case 'LowStock':    return <PackageX size={14} className="text-amber-500" />;
    case 'OverdueDebt': return <AlertTriangle size={14} className="text-red-500" />;
    case 'DueSoon':     return <Clock size={14} className="text-orange-500" />;
    case 'LowProfit':   return <TrendingDown size={14} className="text-rose-500" />;
    case 'AbnormalTx':  return <Activity size={14} className="text-purple-500" />;
    default:            return <Bell size={14} className="text-slate-400" />;
  }
};

export const AlertBell: React.FC = () => {
  const [open, setOpen] = useState(false);
  const [alerts, setAlerts] = useState<Alert[]>([]);
  const [unreadCount, setUnreadCount] = useState(0);
  const [expanded, setExpanded] = useState<number | null>(null);
  const dropdownRef = useRef<HTMLDivElement>(null);
  const { role } = useAuth();

  const fetchAlerts = async () => {
    try {
      const res = await api.get('/alerts');
      setAlerts(res.data.alerts);
      setUnreadCount(res.data.unreadCount);
    } catch { /* bỏ qua nếu chưa có quyền */ }
  };

  useEffect(() => {
    fetchAlerts();
    const id = setInterval(fetchAlerts, 30_000); // Poll mỗi 30 giây
    return () => clearInterval(id);
  }, []);

  // Đóng dropdown khi click ra ngoài
  useEffect(() => {
    const handler = (e: MouseEvent) => {
      if (dropdownRef.current && !dropdownRef.current.contains(e.target as Node)) {
        setOpen(false);
      }
    };
    document.addEventListener('mousedown', handler);
    return () => document.removeEventListener('mousedown', handler);
  }, []);

  const markRead = async (id: number) => {
    await api.post(`/alerts/${id}/read`);
    setAlerts(prev => prev.map(a => a.id === id ? { ...a, isRead: true } : a));
    setUnreadCount(prev => Math.max(0, prev - 1));
  };

  const markAllRead = async () => {
    await api.post('/alerts/read-all');
    setAlerts(prev => prev.map(a => ({ ...a, isRead: true })));
    setUnreadCount(0);
  };

  const checkNow = async () => {
    await api.post('/alerts/check-now');
    setTimeout(fetchAlerts, 1000);
  };

  const fmtDate = (d: string) =>
    new Date(d).toLocaleString('vi-VN', { dateStyle: 'short', timeStyle: 'short' });

  return (
    <div className="relative" ref={dropdownRef}>
      {/* Bell Button */}
      <button
        onClick={() => setOpen(!open)}
        className="relative p-2 rounded-xl text-slate-500 hover:text-indigo-600 hover:bg-indigo-50 transition cursor-pointer"
      >
        <Bell size={20} />
        {unreadCount > 0 && (
          <span className="absolute -top-0.5 -right-0.5 min-w-[18px] h-[18px] bg-red-500 text-white text-[10px] font-extrabold rounded-full flex items-center justify-center px-1 animate-pulse">
            {unreadCount > 9 ? '9+' : unreadCount}
          </span>
        )}
      </button>

      {/* Dropdown */}
      {open && (
        <div className="absolute right-0 top-12 w-96 bg-white rounded-2xl shadow-2xl border border-slate-100 z-50 overflow-hidden">
          {/* Header */}
          <div className="flex items-center justify-between px-5 py-4 border-b border-slate-100">
            <div className="flex items-center gap-2">
              <Bell size={16} className="text-indigo-600" />
              <span className="font-bold text-slate-800">Cảnh Báo</span>
              {unreadCount > 0 && (
                <span className="text-xs bg-red-100 text-red-600 font-bold px-2 py-0.5 rounded-full">{unreadCount} mới</span>
              )}
            </div>
            <div className="flex gap-1">
              {role === 'Admin' && (
                <button onClick={checkNow} className="text-xs text-indigo-500 hover:text-indigo-700 px-2 py-1 rounded-lg hover:bg-indigo-50 cursor-pointer">
                  🔄 Kiểm tra
                </button>
              )}
              {unreadCount > 0 && (
                <button onClick={markAllRead} className="flex items-center gap-1 text-xs text-slate-500 hover:text-slate-700 px-2 py-1 rounded-lg hover:bg-slate-50 cursor-pointer">
                  <CheckCheck size={13}/> Đọc tất
                </button>
              )}
            </div>
          </div>

          {/* Alert List */}
          <div className="max-h-96 overflow-y-auto divide-y divide-slate-50">
            {alerts.length === 0 ? (
              <div className="py-10 text-center text-slate-400">
                <Bell size={28} className="mx-auto mb-2 opacity-30" />
                <p className="text-sm">Không có cảnh báo nào</p>
              </div>
            ) : (
              alerts.map(alert => (
                <div key={alert.id}
                  className={`px-5 py-3.5 cursor-pointer transition-colors ${!alert.isRead ? 'bg-indigo-50/50' : 'hover:bg-slate-50'}`}
                  onClick={() => setExpanded(expanded === alert.id ? null : alert.id)}
                >
                  <div className="flex items-start gap-3">
                    {/* Dot chưa đọc */}
                    <div className="mt-1 shrink-0">
                      {!alert.isRead
                        ? <div className="w-2 h-2 rounded-full bg-indigo-500" />
                        : <div className="w-2 h-2 rounded-full bg-transparent" />
                      }
                    </div>

                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2 mb-0.5">
                        {typeIcon(alert.type)}
                        <span className={`text-xs font-extrabold uppercase tracking-wide ${
                          alert.severity === 'Critical' ? 'text-red-600' : 'text-amber-600'
                        }`}>
                          {alert.severity === 'Critical' ? '🚨 Nghiêm trọng' : '⚠️ Cảnh báo'}
                        </span>
                      </div>
                      <p className="text-sm font-semibold text-slate-800 truncate">{alert.title}</p>
                      <p className="text-xs text-slate-400 mt-0.5">{fmtDate(alert.createdAt)}</p>

                      {/* Expand nội dung */}
                      {expanded === alert.id && (
                        <p className="text-xs text-slate-600 mt-2 whitespace-pre-line bg-slate-50 rounded-lg p-2">
                          {alert.message}
                        </p>
                      )}
                    </div>

                    {/* Nút đánh dấu đã đọc */}
                    {!alert.isRead && (
                      <button
                        onClick={e => { e.stopPropagation(); markRead(alert.id); }}
                        className="shrink-0 p-1 text-slate-300 hover:text-indigo-500 rounded-lg hover:bg-indigo-50 cursor-pointer"
                      >
                        <X size={14}/>
                      </button>
                    )}
                  </div>
                </div>
              ))
            )}
          </div>
        </div>
      )}
    </div>
  );
};
