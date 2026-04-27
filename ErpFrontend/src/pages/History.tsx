import React, { useEffect, useState } from 'react';
import api from '../api/axios';
import { PackagePlus, ShoppingBag, FileText, ArrowRightLeft } from 'lucide-react';
import { useAuth } from '../context/AuthContext';

type Tab = 'stock' | 'sales' | 'transfer';

interface StockLog {
  id: number;
  type: 'Import' | 'Export';
  quantity: number;
  referenceId: string;
  createdBy: string;
  createdAt: string;
  productName: string;
  branchName: string;
}

interface TransferLog {
  id: number;
  productName: string;
  fromBranchName: string;
  toBranchName: string;
  quantity: number;
  status: string;
  createdAt: string;
}

interface Invoice {
  id: number;
  invoiceNumber: string;
  customerName: string;
  invoiceDate: string;
  totalAmount: number;
  paidAmount: number;
  status: string;
  createdBy: string;
}

export const History: React.FC = () => {
  const [tab, setTab] = useState<Tab>('stock');
  const [stockLogs, setStockLogs] = useState<StockLog[]>([]);
  const [invoices, setInvoices] = useState<Invoice[]>([]);
  const [transfers, setTransfers] = useState<TransferLog[]>([]);
  const [loading, setLoading] = useState(true);
  const { role } = useAuth();

  // Payment modal state
  const [payingInvoice, setPayingInvoice] = useState<Invoice | null>(null);
  const [payAmount, setPayAmount] = useState(0);
  const [payMethod, setPayMethod] = useState('Tiền mặt');
  const [paying, setPaying] = useState(false);
  const [payError, setPayError] = useState('');

  // PDF state
  const [exportingPdf, setExportingPdf] = useState<number | null>(null);

  const handleExportPdf = async (invoice: Invoice) => {
    setExportingPdf(invoice.id);
    try {
      const response = await api.get(`/invoices/${invoice.id}/pdf`, {
        responseType: 'blob',
      });
      const url = window.URL.createObjectURL(new Blob([response.data]));
      const link = document.createElement('a');
      link.href = url;
      link.setAttribute('download', `HoaDon_${invoice.invoiceNumber}.pdf`);
      document.body.appendChild(link);
      link.click();
      link.remove();
    } catch (err) {
      console.error('Lỗi xuất PDF:', err);
      alert('Không thể xuất PDF vào lúc này!');
    } finally {
      setExportingPdf(null);
    }
  };

  // Admin: xem chi tiết lịch sử thanh toán từng đơn
  const [expandedInvoiceId, setExpandedInvoiceId] = useState<number | null>(null);
  const [paymentLogs, setPaymentLogs] = useState<Record<number, any[]>>({});

  const loadPaymentLogs = async (invoiceId: number) => {
    if (paymentLogs[invoiceId]) {
      // Toggle đóng/mở
      setExpandedInvoiceId(prev => prev === invoiceId ? null : invoiceId);
      return;
    }
    try {
      const res = await api.get(`/invoices/${invoiceId}/payments`);
      setPaymentLogs(prev => ({ ...prev, [invoiceId]: res.data }));
      setExpandedInvoiceId(invoiceId);
    } catch { /* 403 nếu không phải Admin */ }
  };

  useEffect(() => {
    Promise.all([
      api.get('/stock/history'),
      api.get('/invoices'),
      api.get('/stocktransfer/history'),
    ]).then(([s, i, t]) => {
      setStockLogs(s.data);
      setInvoices(i.data);
      setTransfers(t.data);
      setLoading(false);
    });
  }, []);

  const reloadInvoices = async () => {
    const res = await api.get('/invoices');
    setInvoices(res.data);
  };

  const handlePayment = async () => {
    if (!payingInvoice) return;
    setPaying(true);
    setPayError('');
    try {
      await api.post(`/invoices/${payingInvoice.id}/payments`, {
        amount: payAmount,
        paymentMethod: payMethod,
      });
      setPayingInvoice(null);
      await reloadInvoices();
    } catch (err: any) {
      setPayError(err.response?.data?.error || 'Lỗi ghi thanh toán!');
    } finally {
      setPaying(false);
    }
  };

  const fmt = (n: number) => new Intl.NumberFormat('vi-VN').format(n) + ' ₫';
  const fmtDate = (d: string) =>
    new Date(d).toLocaleString('vi-VN', { dateStyle: 'short', timeStyle: 'short' });

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-slate-800">Lịch Sử Hoạt Động</h1>
        <p className="text-slate-400 text-sm mt-1">Mọi thứ đều được ghi lại — không mất, không quên.</p>
      </div>

      {/* Tabs */}
      <div className="flex gap-2 bg-slate-100 p-1 rounded-xl w-fit">
        <button
          onClick={() => setTab('stock')}
          className={`flex items-center gap-2 px-5 py-2.5 rounded-lg text-sm font-semibold transition-all cursor-pointer ${
            tab === 'stock'
              ? 'bg-white text-indigo-600 shadow-sm'
              : 'text-slate-500 hover:text-slate-700'
          }`}
        >
          <PackagePlus size={16} />
          Nhập / Xuất Kho
        </button>
        <button
          onClick={() => setTab('sales')}
          className={`flex items-center gap-2 px-5 py-2.5 rounded-lg text-sm font-semibold transition-all cursor-pointer ${
            tab === 'sales'
              ? 'bg-white text-indigo-600 shadow-sm'
              : 'text-slate-500 hover:text-slate-700'
          }`}
        >
          <ShoppingBag size={16} />
          Bán Hàng
        </button>
        <button
          onClick={() => setTab('transfer')}
          className={`flex items-center gap-2 px-5 py-2.5 rounded-lg text-sm font-semibold transition-all cursor-pointer ${
            tab === 'transfer'
              ? 'bg-white text-indigo-600 shadow-sm'
              : 'text-slate-500 hover:text-slate-700'
          }`}
        >
          <ArrowRightLeft size={16} />
          Điều Chuyển
        </button>
      </div>

      {/* Stock History Tab */}
      {tab === 'stock' && (
        <div className="bg-white rounded-2xl border border-slate-100 shadow-sm overflow-hidden">
          <div className="px-6 py-4 border-b border-slate-100 flex items-center justify-between">
            <h3 className="font-semibold text-slate-700">Lịch Sử Biến Động Kho</h3>
            <span className="text-xs text-slate-400 bg-slate-100 px-3 py-1 rounded-full">
              {stockLogs.length} giao dịch
            </span>
          </div>
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead className="bg-slate-50">
                <tr>
                {['Thời gian', 'Kho', 'Sản phẩm', 'Loại', 'Số lượng', 'Tham chiếu', ...((role === 'Admin' || role === 'Manager') ? ['Người thực hiện'] : [])].map(h => (
                    <th key={h} className="text-left text-xs font-bold text-slate-500 uppercase px-6 py-3">{h}</th>
                  ))}
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-50">
                {loading ? (
                  <tr><td colSpan={5} className="text-center py-12 text-slate-400 animate-pulse">Đang tải...</td></tr>
                ) : stockLogs.length === 0 ? (
                  <tr><td colSpan={5} className="text-center py-12 text-slate-400">Chưa có giao dịch kho nào.</td></tr>
                ) : stockLogs.map(log => (
                  <tr key={log.id} className="hover:bg-slate-50/60 transition-colors">
                    <td className="px-6 py-3 text-sm text-slate-500 whitespace-nowrap">{fmtDate(log.createdAt)}</td>
                    <td className="px-6 py-3 text-sm font-semibold text-indigo-600 whitespace-nowrap">{log.branchName.replace(/chi nhánh\s+/i, '')}</td>
                    <td className="px-6 py-3 font-medium text-slate-800">{log.productName}</td>
                    <td className="px-6 py-3">
                      <span className={`text-xs font-bold px-2.5 py-1 rounded-full ${
                        log.type === 'Import'
                          ? 'bg-emerald-100 text-emerald-700'
                          : 'bg-orange-100 text-orange-600'
                      }`}>
                        {log.type === 'Import' ? '▲ Nhập' : '▼ Xuất'}
                      </span>
                    </td>
                    <td className="px-6 py-3">
                      <span className={`font-bold text-sm ${
                        log.type === 'Import' ? 'text-emerald-600' : 'text-orange-600'
                      }`}>
                        {log.type === 'Import' ? '+' : '-'}{log.quantity}
                      </span>
                    </td>
                    <td className="px-6 py-3 text-sm text-slate-500 font-mono">{log.referenceId}</td>
                    {(role === 'Admin' || role === 'Manager') && (
                      <td className="px-6 py-3">
                        <span className="inline-flex items-center gap-1.5 text-xs font-semibold bg-indigo-50 text-indigo-700 px-2.5 py-1 rounded-full">
                          👤 {log.createdBy}
                        </span>
                      </td>
                    )}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Sales History Tab */}
      {tab === 'sales' && (
        <div className="bg-white rounded-2xl border border-slate-100 shadow-sm overflow-hidden">
          <div className="px-6 py-4 border-b border-slate-100 flex items-center justify-between">
            <h3 className="font-semibold text-slate-700">Lịch Sử Bán Hàng</h3>
            <span className="text-xs text-slate-400 bg-slate-100 px-3 py-1 rounded-full">
              {invoices.length} hóa đơn
            </span>
          </div>
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead className="bg-slate-50">
                <tr>
                  {['Mã HĐ', 'Khách hàng', 'Ngày bán', 'Tổng tiền', 'Đã trả', 'Trạng thái',
                    ...((role === 'Admin' || role === 'Manager') ? ['Nhân viên bán'] : []),
                    ...((role === 'Accountant' || role === 'Admin' || role === 'Manager') ? ['Thanh toán'] : [])
                  ].map(h => (
                    <th key={h} className="text-left text-xs font-bold text-slate-500 uppercase px-6 py-3">{h}</th>
                  ))}
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-50">
                {loading ? (
                  <tr><td colSpan={6} className="text-center py-12 text-slate-400 animate-pulse">Đang tải...</td></tr>
                ) : invoices.length === 0 ? (
                  <tr><td colSpan={6} className="text-center py-12 text-slate-400">Chưa có hóa đơn nào.</td></tr>
                ) : invoices.map(inv => (
                  <React.Fragment key={inv.id}>
                    <tr className="hover:bg-slate-50/60 transition-colors">
                      <td className="px-6 py-3">
                        <div className="flex items-center gap-3">
                          <span className="font-mono font-bold text-indigo-600">{inv.invoiceNumber}</span>
                          <button
                            onClick={() => handleExportPdf(inv)}
                            disabled={exportingPdf === inv.id}
                            className="p-1 text-slate-400 hover:text-indigo-600 hover:bg-indigo-50 rounded transition-colors cursor-pointer disabled:opacity-50"
                            title="Tải PDF"
                          >
                            <FileText size={16} />
                          </button>
                        </div>
                      </td>
                      <td className="px-6 py-3 font-medium text-slate-800">{inv.customerName}</td>
                      <td className="px-6 py-3 text-sm text-slate-500 whitespace-nowrap">{fmtDate(inv.invoiceDate)}</td>
                      <td className="px-6 py-3 font-bold text-slate-800">{fmt(inv.totalAmount)}</td>
                      <td className="px-6 py-3 text-sm text-slate-600">{fmt(inv.paidAmount)}</td>
                      <td className="px-6 py-3">
                        <span className={`text-xs font-bold px-2.5 py-1 rounded-full ${
                          inv.status === 'Paid'
                            ? 'bg-emerald-100 text-emerald-700'
                            : new Date(inv.invoiceDate) < new Date(Date.now() - 7 * 86400000)
                            ? 'bg-red-100 text-red-600'
                            : 'bg-amber-100 text-amber-600'
                        }`}>
                          {inv.status === 'Paid' ? '✓ Đã TT' : '⏳ Chưa TT'}
                        </span>
                      </td>
                      {(role === 'Admin' || role === 'Manager') && (
                        <td className="px-6 py-3">
                          <span className="inline-flex items-center gap-1.5 text-xs font-semibold bg-slate-100 text-slate-600 px-2.5 py-1 rounded-full">
                            👤 {inv.createdBy}
                          </span>
                        </td>
                      )}
                      {(role === 'Accountant' || role === 'Admin' || role === 'Manager') && (
                        <td className="px-6 py-3">
                          {inv.status !== 'Paid' ? (
                            <button
                              onClick={() => { setPayingInvoice(inv); setPayAmount(inv.totalAmount - inv.paidAmount); setPayError(''); }}
                              className="text-xs font-bold bg-emerald-600 hover:bg-emerald-700 text-white px-3 py-1.5 rounded-lg cursor-pointer transition whitespace-nowrap"
                            >
                              + Ghi TT
                            </button>
                          ) : (
                            <span className="text-xs text-slate-300 italic">Hoàn tất</span>
                          )}
                        </td>
                      )}
                      {(role === 'Admin' || role === 'Manager') && (
                        <td className="px-6 py-3">
                          <button
                            onClick={() => loadPaymentLogs(inv.id)}
                            className="text-xs font-semibold text-indigo-600 hover:text-indigo-800 hover:underline cursor-pointer whitespace-nowrap"
                          >
                            {expandedInvoiceId === inv.id ? '▲ Ẩn' : '👁 Chi tiết'}
                          </button>
                        </td>
                      )}
                    </tr>
                    {(role === 'Admin' || role === 'Manager') && expandedInvoiceId === inv.id && (
                      <tr>
                        <td colSpan={9} className="bg-indigo-50/60 px-8 py-4">
                          <p className="text-xs font-bold text-indigo-600 uppercase tracking-wide mb-3">
                            Lịch sử thanh toán — {inv.invoiceNumber}
                          </p>
                          {(paymentLogs[inv.id] ?? []).length === 0 ? (
                            <p className="text-sm text-slate-400 italic">Chưa có lần thu tiền nào.</p>
                          ) : (
                            <table className="w-full text-sm">
                              <thead>
                                <tr className="text-xs text-slate-500 uppercase">
                                  <th className="text-left pb-2 pr-6">Thời gian</th>
                                  <th className="text-left pb-2 pr-6">Số tiền</th>
                                  <th className="text-left pb-2 pr-6">Phương thức</th>
                                  <th className="text-left pb-2">Người thu</th>
                                </tr>
                              </thead>
                              <tbody className="divide-y divide-indigo-100">
                                {paymentLogs[inv.id].map((p: any) => (
                                  <tr key={p.id}>
                                    <td className="py-2 pr-6 text-slate-500">{fmtDate(p.paymentDate)}</td>
                                    <td className="py-2 pr-6 font-bold text-emerald-700">{fmt(p.amount)}</td>
                                    <td className="py-2 pr-6 text-slate-600">{p.paymentMethod}</td>
                                    <td className="py-2">
                                      <span className="inline-flex items-center gap-1 text-xs font-semibold bg-indigo-100 text-indigo-700 px-2.5 py-1 rounded-full">
                                        👤 {p.processedBy}
                                      </span>
                                    </td>
                                  </tr>
                                ))}
                              </tbody>
                            </table>
                          )}
                        </td>
                      </tr>
                    )}
                  </React.Fragment>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* ── Payment Modal ─────────────────────────────────── */}
      {payingInvoice && (
        <div className="fixed inset-0 bg-black/40 backdrop-blur-sm flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-3xl shadow-2xl w-full max-w-sm p-8 space-y-4">
            <div>
              <h2 className="text-xl font-bold text-slate-800">Ghi Nhận Thanh Toán</h2>
              <p className="text-sm text-slate-400 mt-0.5">
                {payingInvoice.invoiceNumber} — {payingInvoice.customerName}
              </p>
            </div>

            {/* Còn nợ */}
            <div className="flex justify-between items-center bg-rose-50 rounded-2xl p-4">
              <span className="text-sm text-slate-500">Còn nợ</span>
              <span className="text-2xl font-extrabold text-rose-600">
                {fmt(payingInvoice.totalAmount - payingInvoice.paidAmount)}
              </span>
            </div>

            {/* Nhập số tiền */}
            <div>
              <label className="text-sm font-bold text-slate-600 block mb-1.5">Số tiền thanh toán (₫)</label>
              <input
                type="number"
                min={1}
                max={payingInvoice.totalAmount - payingInvoice.paidAmount}
                value={payAmount}
                onChange={e => setPayAmount(Number(e.target.value))}
                className="w-full border border-slate-200 rounded-xl py-3 px-4 text-2xl font-bold text-center focus:outline-none focus:ring-2 focus:ring-emerald-300"
              />
            </div>

            {/* Phương thức */}
            <div>
              <label className="text-sm font-bold text-slate-600 block mb-1.5">Phương thức</label>
              <div className="flex gap-2">
                {['Tiền mặt', 'Chuyển khoản', 'Thẻ'].map(m => (
                  <button
                    key={m}
                    onClick={() => setPayMethod(m)}
                    className={`flex-1 py-2.5 rounded-xl text-sm font-semibold border transition cursor-pointer ${
                      payMethod === m
                        ? 'bg-emerald-600 text-white border-emerald-600'
                        : 'bg-white text-slate-600 border-slate-200 hover:border-emerald-400'
                    }`}
                  >
                    {m}
                  </button>
                ))}
              </div>
            </div>

            {payError && (
              <div className="bg-red-50 text-red-600 p-3 rounded-xl text-sm font-medium">{payError}</div>
            )}

            <div className="flex gap-3 pt-1">
              <button
                onClick={() => setPayingInvoice(null)}
                className="flex-1 py-3 border border-slate-200 rounded-xl text-slate-600 font-semibold hover:bg-slate-50 transition cursor-pointer"
              >
                Hủy
              </button>
              <button
                onClick={handlePayment}
                disabled={paying || payAmount <= 0}
                className="flex-1 py-3 bg-emerald-600 hover:bg-emerald-700 text-white font-bold rounded-xl shadow-lg shadow-emerald-100 hover:-translate-y-0.5 transition-all cursor-pointer disabled:opacity-60 disabled:hover:translate-y-0"
              >
                {paying ? 'Đang lưu...' : '✓ Xác Nhận'}
              </button>
            </div>
          </div>
        </div>
      )}
      {/* Transfer History Tab */}
      {tab === 'transfer' && (
        <div className="bg-white rounded-2xl border border-slate-100 shadow-sm overflow-hidden">
          <div className="px-6 py-4 border-b border-slate-100 flex items-center justify-between">
            <h3 className="font-semibold text-slate-700">Lịch Sử Điều Chuyển Hàng</h3>
            <span className="text-xs text-slate-400 bg-slate-100 px-3 py-1 rounded-full">
              {transfers.length} giao dịch
            </span>
          </div>
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead className="bg-slate-50">
                <tr>
                  {['Thời gian', 'Sản phẩm', 'Từ kho', 'Đến kho', 'Số lượng', 'Trạng thái'].map(h => (
                    <th key={h} className="text-left text-xs font-bold text-slate-500 uppercase px-6 py-3">{h}</th>
                  ))}
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-50">
                {loading ? (
                  <tr><td colSpan={6} className="text-center py-12 text-slate-400 animate-pulse">Đang tải...</td></tr>
                ) : transfers.length === 0 ? (
                  <tr><td colSpan={6} className="text-center py-12 text-slate-400">Chưa có giao dịch điều chuyển nào.</td></tr>
                ) : transfers.map(tx => (
                  <tr key={tx.id} className="hover:bg-slate-50/60 transition-colors">
                    <td className="px-6 py-3 text-sm text-slate-500 whitespace-nowrap">{fmtDate(tx.createdAt)}</td>
                    <td className="px-6 py-3 font-medium text-slate-800">{tx.productName}</td>
                    <td className="px-6 py-3 text-sm text-orange-600 font-semibold">{tx.fromBranchName.replace(/chi nhánh\s+/i, '')}</td>
                    <td className="px-6 py-3 text-sm text-emerald-600 font-semibold">{tx.toBranchName.replace(/chi nhánh\s+/i, '')}</td>
                    <td className="px-6 py-3 font-bold text-slate-700">{tx.quantity}</td>
                    <td className="px-6 py-3">
                      <span className="text-xs font-bold bg-indigo-100 text-indigo-700 px-2.5 py-1 rounded-full">
                        {tx.status}
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  );
};
