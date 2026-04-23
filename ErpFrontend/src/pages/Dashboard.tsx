import React, { useEffect, useState, useCallback } from 'react';
import { useLocation } from 'react-router-dom';
import api from '../api/axios';
import { TrendingUp, AlertTriangle, Clock, ShoppingCart, RefreshCw } from 'lucide-react';

interface RevenueData {
  totalRevenue: number;
  totalInvoices: number;
  averagePerDay: number;
}

interface LowStockProduct {
  id: number;
  name: string;
  stock: number;
  minStockThreshold: number;
}

interface DebtOverview {
  customerName: string;
  totalDebt: number;
  invoiceCount: number;
}

const StatCard: React.FC<{
  title: string;
  value: string;
  sub: string;
  icon: React.ReactNode;
  color: string;
}> = ({ title, value, sub, icon, color }) => (
  <div className="bg-white rounded-2xl p-6 border border-slate-100 shadow-sm hover:shadow-md transition-shadow">
    <div className="flex items-start justify-between mb-4">
      <div className={`w-11 h-11 rounded-xl flex items-center justify-center ${color}`}>
        {icon}
      </div>
    </div>
    <p className="text-slate-500 text-sm font-medium">{title}</p>
    <p className="text-3xl font-extrabold text-slate-800 mt-1">{value}</p>
    <p className="text-slate-400 text-xs mt-1">{sub}</p>
  </div>
);

export const Dashboard: React.FC = () => {
  const [revenue, setRevenue] = useState<RevenueData | null>(null);
  const [lowStock, setLowStock] = useState<LowStockProduct[]>([]);
  const [debts, setDebts] = useState<DebtOverview[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const location = useLocation();

  const fetchData = useCallback(async (isManual = false) => {
    if (isManual) setRefreshing(true);
    const today = new Date().toISOString().split('T')[0];
    const [rev, stock, debt] = await Promise.all([
      api.get(`/reports/revenue?from=${today}&to=${today}`).catch(() => null),
      api.get('/stock/low-stock').catch(() => null),
      api.get('/debt/overview').catch(() => null),
    ]);
    if (rev) setRevenue(rev.data);
    if (stock) setLowStock(stock.data);
    if (debt) setDebts(debt.data);
    setLoading(false);
    setRefreshing(false);
  }, []);

  // Re-fetch mỗi khi navigate về Dashboard hoặc sau 60 giây
  useEffect(() => { fetchData(); }, [location.pathname, fetchData]);
  useEffect(() => {
    const id = setInterval(() => fetchData(), 60_000);
    return () => clearInterval(id);
  }, [fetchData]);

  const formatMoney = (n: number) =>
    new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(n);

  if (loading)
    return (
      <div className="flex items-center justify-center h-full">
        <div className="text-slate-400 text-lg animate-pulse">Đang tải dữ liệu...</div>
      </div>
    );

  return (
    <div className="space-y-8">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-slate-800">Tổng Quan Hôm Nay</h1>
          <p className="text-slate-400 text-sm mt-1">Nhìn một cái biết liền, không cần hỏi thêm.</p>
        </div>
        <button
          onClick={() => fetchData(true)}
          disabled={refreshing}
          className="flex items-center gap-2 text-sm font-semibold text-slate-500 hover:text-indigo-600 bg-white border border-slate-200 px-4 py-2 rounded-xl hover:border-indigo-300 transition-all cursor-pointer disabled:opacity-50"
        >
          <RefreshCw size={15} className={refreshing ? 'animate-spin' : ''} />
          {refreshing ? 'Đang tải...' : 'Làm mới'}
        </button>
      </div>

      {/* Stat Cards */}
      <div className="grid grid-cols-4 gap-5">
        <StatCard
          title="Doanh thu hôm nay"
          value={formatMoney(revenue?.totalRevenue ?? 0)}
          sub={`${revenue?.totalInvoices ?? 0} hóa đơn đã thanh toán`}
          icon={<TrendingUp size={20} className="text-emerald-600" />}
          color="bg-emerald-50"
        />
        <StatCard
          title="Hàng sắp hết kho"
          value={`${lowStock.length} SKU`}
          sub={lowStock.length > 0 ? `Cần nhập hàng ngay!` : 'Tồn kho ổn định'}
          icon={<AlertTriangle size={20} className={lowStock.length > 0 ? "text-amber-500" : "text-slate-400"} />}
          color={lowStock.length > 0 ? "bg-amber-50" : "bg-slate-50"}
        />
        <StatCard
          title="Tổng công nợ"
          value={formatMoney(debts.reduce((s, d) => s + d.totalDebt, 0))}
          sub={`${debts.length} khách hàng còn nợ`}
          icon={<Clock size={20} className="text-rose-500" />}
          color="bg-rose-50"
        />
        <StatCard
          title="Khách hàng nợ"
          value={`${debts.length}`}
          sub="Danh sách phía dưới"
          icon={<ShoppingCart size={20} className="text-indigo-500" />}
          color="bg-indigo-50"
        />
      </div>

      {/* Low Stock & Debt Tables */}
      <div className="grid grid-cols-2 gap-6">
        {/* Low Stock */}
        <div className="bg-white rounded-2xl border border-slate-100 shadow-sm overflow-hidden">
          <div className="px-6 py-4 border-b border-slate-100 flex items-center gap-2">
            <AlertTriangle size={16} className="text-amber-500" />
            <h3 className="font-semibold text-slate-700">Hàng Sắp Hết Kho</h3>
          </div>
          <div className="divide-y divide-slate-50">
            {lowStock.length === 0 ? (
              <p className="px-6 py-8 text-center text-slate-400 text-sm">✅ Tất cả tồn kho đang ổn!</p>
            ) : lowStock.map((p) => (
              <div key={p.id} className="px-6 py-3 flex items-center justify-between hover:bg-slate-50 transition-colors">
                <span className="text-sm font-medium text-slate-700">{p.name}</span>
                <span className={`text-sm font-bold px-2.5 py-1 rounded-full ${p.stock <= 3 ? 'bg-red-100 text-red-600' : 'bg-amber-100 text-amber-600'}`}>
                  Còn {p.stock}
                </span>
              </div>
            ))}
          </div>
        </div>

        {/* Debt Overview */}
        <div className="bg-white rounded-2xl border border-slate-100 shadow-sm overflow-hidden">
          <div className="px-6 py-4 border-b border-slate-100 flex items-center gap-2">
            <Clock size={16} className="text-rose-500" />
            <h3 className="font-semibold text-slate-700">Công Nợ Khách Hàng</h3>
          </div>
          <div className="divide-y divide-slate-50">
            {debts.length === 0 ? (
              <p className="px-6 py-8 text-center text-slate-400 text-sm">✅ Không có công nợ tồn đọng!</p>
            ) : debts.map((d) => (
              <div key={d.customerName} className="px-6 py-3 flex items-center justify-between hover:bg-slate-50 transition-colors">
                <div>
                  <span className="text-sm font-medium text-slate-700">{d.customerName}</span>
                  <span className="text-xs text-slate-400 ml-2">{d.invoiceCount} hóa đơn</span>
                </div>
                <span className="text-sm font-bold text-rose-600">{formatMoney(d.totalDebt)}</span>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
};
