import React, { useEffect, useState } from 'react';
import api from '../api/axios';
import { Plus, Trash2, X, Check, PackagePlus, Download, Upload, RotateCcw, ArrowRightLeft } from 'lucide-react';
import { useAuth } from '../context/AuthContext';

interface BranchStock {
  branchId: number;
  branch?: { name: string };
  quantity: number;
}

interface Product {
  id: number;
  productCode: string;
  name: string;
  purchasePrice: number;
  price: number;
  stock: number;
  minStockThreshold: number;
  branchStocks: BranchStock[];
}

const emptyProduct = { productCode: '', name: '', purchasePrice: 0, price: 0, stock: 0, minStockThreshold: 5 };

export const Products: React.FC = () => {
  const [products, setProducts] = useState<Product[]>([]);
  const [loading, setLoading] = useState(true);
  const [showModal, setShowModal] = useState(false);
  const [form, setForm] = useState(emptyProduct);
  const [saving, setSaving] = useState(false);
  const [exporting, setExporting] = useState(false);
  const [importing, setImporting] = useState(false);
  const [undoing, setUndoing] = useState(false);
  const [lastBatchId, setLastBatchId] = useState<string | null>(null);
  const [error, setError] = useState('');
  const { role, branchId } = useAuth();

  const fileInputRef = React.useRef<HTMLInputElement>(null);

  // Restock modal state
  const [restockProduct, setRestockProduct] = useState<Product | null>(null);
  const [restockQty, setRestockQty] = useState(1);
  const [restockNote, setRestockNote] = useState('');
  const [restockSaving, setRestockSaving] = useState(false);
  const [restockMsg, setRestockMsg] = useState('');
  
  // Transfer modal state
  const [branches, setBranches] = useState<any[]>([]);
  const [transferProduct, setTransferProduct] = useState<Product | null>(null);
  const [transferFromBranch, setTransferFromBranch] = useState('');
  const [transferToBranch, setTransferToBranch] = useState('');
  const [transferQty, setTransferQty] = useState(1);
  const [transferSaving, setTransferSaving] = useState(false);
  const [transferMsg, setTransferMsg] = useState('');

  const fetchInitialData = async () => {
    setLoading(true);
    try {
      const [pRes, bRes] = await Promise.all([
        api.get('/products'),
        api.get('/branches') // Need to make sure this endpoint exists
      ]);
      setProducts(pRes.data);
      setBranches(bRes.data);
    } catch (err) {
      console.error('Lỗi tải dữ liệu:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { fetchInitialData(); }, []);

  const handleExportExcel = async () => {
    setExporting(true);
    try {
      const response = await api.get('/products/export/excel', {
        responseType: 'blob',
      });
      
      const url = window.URL.createObjectURL(new Blob([response.data]));
      const link = document.createElement('a');
      link.href = url;
      link.setAttribute('download', `DanhSachSanPham_${new Date().getTime()}.xlsx`);
      document.body.appendChild(link);
      link.click();
      link.remove();
    } catch (err) {
      console.error('Lỗi xuất Excel:', err);
      alert('Không thể xuất Excel vào lúc này!');
    } finally {
      setExporting(false);
    }
  };

  const handleImportExcel = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    setImporting(true);
    setLastBatchId(null);
    const formData = new FormData();
    formData.append('file', file);

    try {
      const res = await api.post('/products/import/excel', formData, {
        headers: { 'Content-Type': 'multipart/form-data' }
      });
      alert(res.data.message + (res.data.errors.length > 0 ? '\nLưu ý: Có một số lỗi ở các dòng: ' + res.data.errors.join(', ') : ''));
      setLastBatchId(res.data.batchId);
      await fetchInitialData();
    } catch (err: any) {
      alert('Lỗi Import: ' + (err.response?.data || 'Không thể đọc file!'));
    } finally {
      setImporting(false);
      if (fileInputRef.current) fileInputRef.current.value = '';
    }
  };

  const handleUndoImport = async () => {
    if (!lastBatchId) return;
    if (!confirm('Bạn có chắc chắn muốn hoàn tác phiên nhập kho vừa rồi? Tồn kho sẽ được trừ lại.')) return;

    setUndoing(true);
    try {
      await api.post(`/products/import/undo/${lastBatchId}`);
      alert('Đã hoàn tác thành công!');
      setLastBatchId(null);
      await fetchInitialData();
    } catch (err: any) {
      alert('Lỗi hoàn tác: ' + (err.response?.data?.message || 'Có lỗi xảy ra!'));
    } finally {
      setUndoing(false);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    setError('');
    try {
      await api.post('/products', form);
      setShowModal(false);
      setForm(emptyProduct);
      await fetchInitialData();
    } catch (err: any) {
      setError(err.response?.data?.error || 'Có lỗi xảy ra!');
    } finally {
      setSaving(false);
    }
  };

  const handleRestock = async () => {
    if (restockQty < 1) return;
    setRestockSaving(true);
    setRestockMsg('');
    try {
      const res = await api.post('/stock/import', {
        productId: restockProduct!.id,
        quantity: restockQty,
        note: restockNote || 'Nhập kho thủ công'
      });
      setRestockMsg(`✅ ${res.data.message} Tồn kho mới: ${res.data.newStock}`);
      setRestockQty(1);
      setRestockNote('');
      await fetchInitialData();
    } catch (err: any) {
      setRestockMsg('❌ ' + (err.response?.data?.error || 'Lỗi nhập kho!'));
    } finally {
      setRestockSaving(false);
    }
  };

  const handleTransfer = async () => {
    if (transferQty < 1 || !transferToBranch) return;
    setTransferSaving(true);
    setTransferMsg('');
    try {
      await api.post('/stocktransfer', {
        productId: transferProduct!.id,
        fromBranchId: role === 'Admin' ? Number(transferFromBranch) : (branchId || 1), 
        toBranchId: Number(transferToBranch),
        quantity: transferQty
      });
      setTransferMsg('✅ Điều chuyển thành công!');
      setTransferQty(1);
      setTimeout(() => {
        setTransferProduct(null);
        setTransferMsg('');
        fetchInitialData();
      }, 1500);
    } catch (err: any) {
      setTransferMsg('❌ ' + (err.response?.data || 'Lỗi điều chuyển!'));
    } finally {
      setTransferSaving(false);
    }
  };

  const formatMoney = (n: number) =>
    new Intl.NumberFormat('vi-VN').format(n) + ' ₫';

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-slate-800">Quản Lý Sản Phẩm</h1>
          <p className="text-slate-400 text-sm mt-1">Thêm, sửa, kiểm tra tồn kho hàng hóa.</p>
        </div>
        <div className="flex gap-3">
          {lastBatchId && (
            <button
              onClick={handleUndoImport}
              disabled={undoing}
              className="flex items-center gap-2 bg-orange-50 border border-orange-200 hover:bg-orange-100 text-orange-700 font-bold py-3 px-5 rounded-xl shadow-sm transition-all cursor-pointer disabled:opacity-50"
            >
              <RotateCcw size={18} />
              {undoing ? 'Đang hoàn tác...' : 'Hoàn tác nhập'}
            </button>
          )}
          <input
            type="file"
            ref={fileInputRef}
            onChange={handleImportExcel}
            className="hidden"
            accept=".xlsx, .xls"
          />
          <button
            onClick={() => fileInputRef.current?.click()}
            disabled={importing}
            className="flex items-center gap-2 bg-white border border-slate-200 hover:bg-slate-50 text-slate-700 font-bold py-3 px-5 rounded-xl shadow-sm transition-all cursor-pointer disabled:opacity-50"
          >
            <Upload size={18} className="text-indigo-600" />
            {importing ? 'Đang nhập...' : 'Nhập Excel'}
          </button>
          <button
            onClick={handleExportExcel}
            disabled={exporting}
            className="flex items-center gap-2 bg-white border border-slate-200 hover:bg-slate-50 text-slate-700 font-bold py-3 px-5 rounded-xl shadow-sm transition-all cursor-pointer disabled:opacity-50"
          >
            <Download size={18} className="text-indigo-600" />
            {exporting ? 'Đang xuất...' : 'Xuất Excel'}
          </button>
          <button
            onClick={() => { setShowModal(true); setForm(emptyProduct); setError(''); }}
            className="flex items-center gap-2 bg-indigo-600 hover:bg-indigo-700 text-white font-bold py-3 px-5 rounded-xl shadow-lg shadow-indigo-200 transform hover:-translate-y-0.5 transition-all cursor-pointer"
          >
            <Plus size={18} />
            Thêm Sản Phẩm
          </button>
        </div>
      </div>

      {/* Table */}
      <div className="bg-white rounded-2xl border border-slate-100 shadow-sm overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead>
              <tr className="bg-slate-50 border-b border-slate-100">
                <th className="text-left text-xs font-bold text-slate-500 uppercase tracking-wider px-6 py-4">Mã SP</th>
                <th className="text-left text-xs font-bold text-slate-500 uppercase tracking-wider px-6 py-4">Tên sản phẩm</th>
                <th className="text-right text-xs font-bold text-slate-500 uppercase tracking-wider px-6 py-4">Giá nhập</th>
                <th className="text-right text-xs font-bold text-slate-500 uppercase tracking-wider px-6 py-4">Giá bán</th>
                <th className="text-right text-xs font-bold text-slate-500 uppercase tracking-wider px-6 py-4">Tồn kho</th>
                {role === 'Admin' && (
                  <th className="text-center text-xs font-bold text-slate-500 uppercase tracking-wider px-6 py-4">Nhập kho</th>
                )}
                {role === 'Admin' && (
                  <th className="text-center text-xs font-bold text-slate-500 uppercase tracking-wider px-6 py-4">Điều chuyển</th>
                )}
                {role === 'Admin' && (
                  <th className="text-center text-xs font-bold text-slate-500 uppercase tracking-wider px-6 py-4">Xóa</th>
                )}
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-50">
              {loading ? (
                <tr><td colSpan={6} className="text-center py-12 text-slate-400 animate-pulse">Đang tải...</td></tr>
              ) : products.length === 0 ? (
                <tr><td colSpan={6} className="text-center py-12 text-slate-400">Chưa có sản phẩm. Hãy thêm mới!</td></tr>
              ) : products.map((p) => (
                <tr key={p.id} className="hover:bg-slate-50/60 transition-colors">
                  <td className="px-6 py-4">
                    <span className="font-mono text-xs font-bold bg-slate-100 text-slate-600 px-2 py-1 rounded">
                      {p.productCode}
                    </span>
                  </td>
                  <td className="px-6 py-4 font-semibold text-slate-800">{p.name}</td>
                  <td className="px-6 py-4 text-right text-sm text-slate-500">{formatMoney(p.purchasePrice)}</td>
                  <td className="px-6 py-4 text-right text-sm font-bold text-indigo-600">{formatMoney(p.price)}</td>
                  <td className="px-6 py-4 text-right">
                    <div className="flex flex-col items-end">
                      <span className={`font-bold px-3 py-1 rounded-full text-sm ${
                        p.stock <= p.minStockThreshold
                          ? 'bg-red-100 text-red-600'
                          : 'bg-emerald-100 text-emerald-700'
                      }`}>
                        {p.stock}
                      </span>
                      {role === 'Admin' && p.branchStocks?.length > 0 && (
                        <div className="text-[10px] text-slate-400 mt-2 flex flex-wrap justify-end gap-1">
                          {p.branchStocks.map(bs => (
                            <span key={bs.branchId} className="bg-slate-100/50 text-slate-500 px-2 py-0.5 rounded-md border border-slate-200/50 whitespace-nowrap font-medium">
                              {bs.branch?.name?.replace(/chi nhánh\s+/i, '') || `Kho ${bs.branchId}`}: <span className="text-indigo-600 font-bold">{bs.quantity}</span>
                            </span>
                          ))}
                        </div>
                      )}
                    </div>
                  </td>
                  {(role === 'Admin' || role === 'Manager' || role === 'User') && (
                  <td className="px-6 py-4 text-center">
                    <button
                      onClick={() => { setRestockProduct(p); setRestockQty(1); setRestockNote(''); setRestockMsg(''); }}
                      className="text-emerald-500 hover:text-emerald-700 p-1.5 hover:bg-emerald-50 rounded-lg transition-colors cursor-pointer"
                      title="Nhập thêm hàng"
                    >
                      <PackagePlus size={16} />
                    </button>
                  </td>
                  )}
                  {role === 'Admin' && (
                  <td className="px-6 py-4 text-center">
                    <button
                      onClick={() => { setTransferProduct(p); setTransferQty(1); setTransferMsg(''); setTransferToBranch(''); }}
                      className="text-indigo-500 hover:text-indigo-700 p-1.5 hover:bg-indigo-50 rounded-lg transition-colors cursor-pointer"
                      title="Điều chuyển hàng"
                    >
                      <ArrowRightLeft size={16} />
                    </button>
                  </td>
                  )}
                  {(role === 'Admin' || role === 'Manager' || role === 'User') && (
                    <td className="px-6 py-4 text-center">
                      <button
                        onClick={() => api.delete(`/products/${p.id}`).then(fetchInitialData)}
                        className="text-red-400 hover:text-red-600 p-1.5 hover:bg-red-50 rounded-lg transition-colors cursor-pointer"
                        title="Xóa sản phẩm"
                      >
                        <Trash2 size={16} />
                      </button>
                    </td>
                  )}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {/* Add Product Modal */}
      {showModal && (
        <div className="fixed inset-0 bg-black/40 backdrop-blur-sm flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-3xl shadow-2xl w-full max-w-md p-8">
            <div className="flex items-center justify-between mb-6">
              <h2 className="text-xl font-bold text-slate-800">Thêm Sản Phẩm Mới</h2>
              <button onClick={() => setShowModal(false)} className="text-slate-400 hover:text-slate-600 cursor-pointer"><X size={22} /></button>
            </div>
            {error && <div className="bg-red-50 text-red-600 p-3 rounded-xl text-sm mb-4">{error}</div>}
            <form onSubmit={handleSubmit} className="space-y-4">
              {[
                { label: 'Mã sản phẩm (SKU)', key: 'productCode', type: 'text', placeholder: 'Tự động sinh nếu để trống' },
                { label: 'Tên sản phẩm', key: 'name', type: 'text', placeholder: 'VD: Bàn phím cơ Keychron' },
                { label: 'Giá nhập (₫)', key: 'purchasePrice', type: 'number', placeholder: '300000' },
                { label: 'Giá bán (₫)', key: 'price', type: 'number', placeholder: '500000' },
                { label: 'Tồn kho ban đầu', key: 'stock', type: 'number', placeholder: '50' },
                { label: 'Ngưỡng cảnh báo tồn kho', key: 'minStockThreshold', type: 'number', placeholder: '5' },
              ].map(({ label, key, type, placeholder }) => (
                <div key={key}>
                  <label className="text-sm font-bold text-slate-600 block mb-1.5">{label}</label>
                  <input
                    type={type}
                    value={(form as any)[key]}
                    onChange={(e) => setForm(f => ({ ...f, [key]: type === 'number' ? Number(e.target.value) : e.target.value }))}
                    placeholder={placeholder}
                    className="w-full border border-slate-200 rounded-xl py-3 px-4 text-slate-800 focus:outline-none focus:ring-2 focus:ring-indigo-300 transition"
                    required={key !== 'productCode'}
                  />
                </div>
              ))}
              <button
                type="submit"
                disabled={saving}
                className="w-full flex items-center justify-center gap-2 bg-indigo-600 hover:bg-indigo-700 text-white font-bold py-3.5 rounded-xl mt-2 transition cursor-pointer disabled:opacity-70"
              >
                <Check size={18} />
                {saving ? 'Đang lưu...' : 'Thêm Ngay'}
              </button>
            </form>
          </div>
        </div>
      )}

      {/* Restock Modal */}
      {restockProduct && (
        <div className="fixed inset-0 bg-black/40 backdrop-blur-sm flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-3xl shadow-2xl w-full max-w-sm p-8">
            <div className="flex items-center justify-between mb-6">
              <div>
                <h2 className="text-xl font-bold text-slate-800">Nhập Thêm Hàng</h2>
                <p className="text-sm text-slate-400 mt-0.5">{restockProduct.name}</p>
              </div>
              <button onClick={() => setRestockProduct(null)} className="text-slate-400 hover:text-slate-600 cursor-pointer"><X size={22} /></button>
            </div>

            <div className="bg-slate-50 rounded-2xl p-4 mb-5 flex justify-between items-center">
              <span className="text-sm text-slate-500">Tồn kho hiện tại</span>
              <span className="text-2xl font-extrabold text-indigo-600">{restockProduct.stock}</span>
            </div>

            <div className="space-y-4">
              <div>
                <label className="text-sm font-bold text-slate-600 block mb-1.5">Số lượng nhập thêm</label>
                <input
                  type="number"
                  min={1}
                  value={restockQty}
                  onChange={e => setRestockQty(Math.max(1, Number(e.target.value)))}
                  className="w-full border border-slate-200 rounded-xl py-3 px-4 text-center text-2xl font-bold text-slate-800 focus:outline-none focus:ring-2 focus:ring-emerald-300"
                />
              </div>
              <div>
                <label className="text-sm font-bold text-slate-600 block mb-1.5">Ghi chú (tuỳ chọn)</label>
                <input
                  type="text"
                  value={restockNote}
                  onChange={e => setRestockNote(e.target.value)}
                  placeholder="VD: Hàng mới về lô tháng 5"
                  className="w-full border border-slate-200 rounded-xl py-3 px-4 text-slate-800 focus:outline-none focus:ring-2 focus:ring-emerald-300"
                />
              </div>
            </div>

            {restockMsg && (
              <div className={`mt-4 p-3 rounded-xl text-sm font-medium ${
                restockMsg.startsWith('✅') ? 'bg-emerald-50 text-emerald-700' : 'bg-red-50 text-red-600'
              }`}>{restockMsg}</div>
            )}

            <button
              onClick={handleRestock}
              disabled={restockSaving}
              className="w-full flex items-center justify-center gap-2 bg-emerald-600 hover:bg-emerald-700 text-white font-bold py-4 rounded-2xl mt-5 shadow-lg shadow-emerald-100 hover:-translate-y-0.5 transition-all cursor-pointer disabled:opacity-70"
            >
              <PackagePlus size={18} />
              {restockSaving ? 'Đang nhập...' : `Nhập thêm ${restockQty} cái`}
            </button>
          </div>
        </div>
      )}
      {/* Transfer Modal */}
      {transferProduct && (
        <div className="fixed inset-0 bg-black/40 backdrop-blur-sm flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-3xl shadow-2xl w-full max-w-md p-8">
            <div className="flex items-center justify-between mb-6">
              <h2 className="text-xl font-bold text-slate-800">Điều Chuyển Hàng</h2>
              <button onClick={() => setTransferProduct(null)} className="text-slate-400 hover:text-slate-600 cursor-pointer"><X size={22} /></button>
            </div>
            
            <div className="mb-6">
              <p className="text-sm text-slate-500">Sản phẩm: <span className="font-bold text-slate-800">{transferProduct.name}</span></p>
              <p className="text-sm text-slate-500">Tồn kho hiện tại: <span className="font-bold text-slate-800">{transferProduct.stock}</span></p>
            </div>

            {transferMsg && <div className={`p-4 rounded-xl text-sm mb-4 ${transferMsg.includes('✅') ? 'bg-emerald-50 text-emerald-700' : 'bg-red-50 text-red-600'}`}>{transferMsg}</div>}

            <div className="space-y-4">
              {role === 'Admin' && (
                <div>
                  <label className="text-sm font-bold text-slate-600 block mb-2">Từ</label>
                  <select 
                    value={transferFromBranch}
                    onChange={e => setTransferFromBranch(e.target.value)}
                    className="w-full border border-slate-200 rounded-xl py-3 px-4 bg-white cursor-pointer focus:outline-none focus:ring-2 focus:ring-indigo-300"
                  >
                    <option value="">-- Chọn nguồn --</option>
                    {branches.map(b => (
                      <option key={b.id} value={b.id}>{b.name.replace(/chi nhánh\s+/i, '')}</option>
                    ))}
                  </select>
                </div>
              )}
              <div>
                <label className="text-sm font-bold text-slate-600 block mb-2">Đến</label>
                <select 
                  value={transferToBranch}
                  onChange={e => setTransferToBranch(e.target.value)}
                  className="w-full border border-slate-200 rounded-xl py-3 px-4 bg-white cursor-pointer focus:outline-none focus:ring-2 focus:ring-indigo-300"
                >
                  <option value="">-- Chọn đích --</option>
                  {branches.map(b => (
                    <option key={b.id} value={b.id}>{b.name.replace(/chi nhánh\s+/i, '')}</option>
                  ))}
                </select>
              </div>
              <div>
                <label className="text-sm font-bold text-slate-600 block mb-2">Số lượng chuyển</label>
                <input 
                  type="number" 
                  min={1} 
                  max={transferProduct.stock}
                  value={transferQty} 
                  onChange={e => setTransferQty(Number(e.target.value))}
                  className="w-full border border-slate-200 rounded-xl py-3 px-4 focus:outline-none focus:ring-2 focus:ring-indigo-300"
                />
              </div>
              <button
                onClick={handleTransfer}
                disabled={transferSaving || !transferToBranch || (role === 'Admin' && !transferFromBranch) || transferQty < 1}
                className="w-full bg-indigo-600 hover:bg-indigo-700 text-white font-bold py-3.5 rounded-xl shadow-lg shadow-indigo-100 transition-all disabled:opacity-50 cursor-pointer"
              >
                {transferSaving ? 'Đang thực hiện...' : 'Xác Nhận Điều Chuyển'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
