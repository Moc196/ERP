import React, { useState } from 'react';
import api from '../api/axios';
import { BarChart3, Download, TrendingUp, Trophy, DollarSign } from 'lucide-react';
import { useAuth } from '../context/AuthContext';

export const Reports: React.FC = () => {
  const [from, setFrom] = useState(() => new Date(Date.now() - 30 * 86400000).toISOString().split('T')[0]);
  const [to, setTo] = useState(() => new Date().toISOString().split('T')[0]);
  const [revenue, setRevenue] = useState<any>(null);
  const [profit, setProfit] = useState<any>(null);
  const [topProducts, setTopProducts] = useState<any[]>([]);
  const [loading, setLoading] = useState(false);
  const { role } = useAuth();

  const fmt = (n: number) => new Intl.NumberFormat('vi-VN').format(n) + ' ₫';

  const fetchReports = async () => {
    setLoading(true);
    const params = `from=${from}&to=${to}`;
    const [rev, prof, top] = await Promise.all([
      api.get(`/reports/revenue?${params}`).catch(() => null),
      api.get(`/reports/profit?${params}`).catch(() => null),
      api.get('/reports/top-products?limit=5').catch(() => null),
    ]);
    if (rev) setRevenue(rev.data);
    if (prof) setProfit(prof.data);
    if (top) setTopProducts(top.data);
    setLoading(false);
  };

  const handleExcelExport = async () => {
    try {
      const response = await api.get('/reports/export/excel?type=revenue', {
        responseType: 'blob'
      });
      const url = window.URL.createObjectURL(new Blob([response.data]));
      const link = document.createElement('a');
      link.href = url;
      link.setAttribute('download', `Report_Revenue_${new Date().toISOString().split('T')[0]}.xlsx`);
      document.body.appendChild(link);
      link.click();
      link.remove();
    } catch (err) {
      alert('Bạn không có quyền tải báo cáo!');
    }
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-slate-800">Báo Cáo & Thống Kê</h1>
          <p className="text-slate-400 text-sm mt-1">Số liệu real-time. Đẹp để khoe sếp.</p>
        </div>
        {(role === 'Admin' || role === 'Manager') && (
          <button onClick={handleExcelExport} className="flex items-center gap-2 bg-emerald-600 hover:bg-emerald-700 text-white font-bold py-3 px-5 rounded-xl shadow-lg shadow-emerald-200 hover:-translate-y-0.5 transition-all cursor-pointer">
            <Download size={18}/> Export Excel
          </button>
        )}
      </div>

      {/* Date Filter */}
      <div className="bg-white rounded-2xl border border-slate-100 shadow-sm p-5 flex items-end gap-4">
        {[{ label: 'Từ ngày', val: from, set: setFrom }, { label: 'Đến ngày', val: to, set: setTo }].map(f => (
          <div key={f.label} className="flex-1">
            <label className="text-sm font-bold text-slate-600 block mb-1.5">{f.label}</label>
            <input type="date" value={f.val} onChange={e => f.set(e.target.value)} className="w-full border border-slate-200 rounded-xl py-3 px-4 focus:outline-none focus:ring-2 focus:ring-indigo-300"/>
          </div>
        ))}
        <button onClick={fetchReports} disabled={loading} className="flex items-center gap-2 bg-indigo-600 hover:bg-indigo-700 text-white font-bold py-3 px-6 rounded-xl transition cursor-pointer disabled:opacity-70">
          <BarChart3 size={18}/>{loading ? 'Đang tải...' : 'Xem Báo Cáo'}
        </button>
      </div>

      {revenue && (
        <div className="grid grid-cols-3 gap-5">
          {[
            { label: 'Tổng doanh thu', val: fmt(revenue.totalRevenue), icon: <TrendingUp size={20} className="text-emerald-600"/>, bg: 'bg-emerald-50' },
            { label: 'Số hóa đơn', val: `${revenue.totalInvoices}`, icon: <BarChart3 size={20} className="text-indigo-600"/>, bg: 'bg-indigo-50' },
            { label: 'TB mỗi ngày', val: fmt(revenue.averagePerDay), icon: <DollarSign size={20} className="text-amber-600"/>, bg: 'bg-amber-50' },
          ].map(c => (
            <div key={c.label} className="bg-white rounded-2xl border border-slate-100 shadow-sm p-6">
              <div className={`w-10 h-10 rounded-xl flex items-center justify-center mb-4 ${c.bg}`}>{c.icon}</div>
              <p className="text-slate-500 text-sm font-medium">{c.label}</p>
              <p className="text-2xl font-extrabold text-slate-800 mt-1">{c.val}</p>
            </div>
          ))}
        </div>
      )}

      <div className="grid grid-cols-2 gap-6">
        {profit && (
          <div className="bg-white rounded-2xl border border-slate-100 shadow-sm overflow-hidden">
            <div className="px-6 py-4 border-b border-slate-100 flex items-center gap-2">
              <DollarSign size={16} className="text-emerald-500"/>
              <h3 className="font-semibold text-slate-700">Lợi Nhuận Chi Tiết</h3>
            </div>
            <div className="p-6 space-y-4">
              <div className="flex justify-between items-center">
                <span className="text-slate-500">Tổng Lợi Nhuận</span>
                <span className="text-2xl font-extrabold text-emerald-600">{fmt(profit.totalProfit)}</span>
              </div>
              <div className="flex justify-between items-center">
                <span className="text-slate-500">Biên Lợi Nhuận</span>
                <span className="font-bold text-slate-700">{profit.profitMargin}%</span>
              </div>
              <div className="mt-4 border-t border-slate-100 pt-4 space-y-2">
                {profit.profitByProduct?.slice(0, 5).map((p: any) => (
                  <div key={p.productId} className="flex justify-between text-sm">
                    <span className="text-slate-600 truncate max-w-[60%]">{p.productName}</span>
                    <span className="font-bold text-emerald-600">{fmt(p.profit)}</span>
                  </div>
                ))}
              </div>
            </div>
          </div>
        )}

        {topProducts.length > 0 && (
          <div className="bg-white rounded-2xl border border-slate-100 shadow-sm overflow-hidden">
            <div className="px-6 py-4 border-b border-slate-100 flex items-center gap-2">
              <Trophy size={16} className="text-amber-500"/>
              <h3 className="font-semibold text-slate-700">Top Sản Phẩm Bán Chạy</h3>
            </div>
            <div className="divide-y divide-slate-50">
              {topProducts.map((p, i) => (
                <div key={p.productId} className="px-6 py-3 flex items-center gap-4">
                  <span className={`w-7 h-7 rounded-full flex items-center justify-center font-bold text-sm ${i === 0 ? 'bg-amber-100 text-amber-600' : i === 1 ? 'bg-slate-100 text-slate-600' : 'bg-orange-100 text-orange-600'}`}>
                    {i + 1}
                  </span>
                  <div className="flex-1">
                    <p className="text-sm font-medium text-slate-800">{p.productName}</p>
                    <p className="text-xs text-slate-400">Đã bán: {p.totalQuantitySold} cái</p>
                  </div>
                  <span className="text-sm font-bold text-indigo-600">{fmt(p.totalRevenueGenerated)}</span>
                </div>
              ))}
            </div>
          </div>
        )}
      </div>
    </div>
  );
};
