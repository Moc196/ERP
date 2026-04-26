import React, { useEffect, useState } from 'react';
import api from '../api/axios';
import { ShoppingBag, Plus, Trash2, Printer, X } from 'lucide-react';

interface Product { id: number; name: string; price: number; stock: number; }
interface InvoiceItemForm { productId: number; quantity: number; productName: string; unitPrice: number; }

export const CreateInvoice: React.FC = () => {
  const [products, setProducts] = useState<Product[]>([]);
  const [customerName, setCustomerName] = useState('');
  const [items, setItems] = useState<InvoiceItemForm[]>([]);
  const [selectedProduct, setSelectedProduct] = useState('');
  const [quantity, setQuantity] = useState(1);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [printInvoice, setPrintInvoice] = useState<any>(null);
  const [currency, setCurrency] = useState('VND');
  const [customers, setCustomers] = useState<any[]>([]);

  useEffect(() => { 
    api.get('/products').then(r => setProducts(r.data)); 
    api.get('/api/customers').then(r => setCustomers(r.data));
  }, []);

  const fmt = (n: number) => new Intl.NumberFormat('vi-VN').format(n) + ' ₫';
  const total = items.reduce((s, i) => s + i.quantity * i.unitPrice, 0);

  const addItem = () => {
    const product = products.find(p => p.id === Number(selectedProduct));
    if (!product) return;
    const idx = items.findIndex(i => i.productId === product.id);
    if (idx >= 0) setItems(items.map((i, j) => j === idx ? { ...i, quantity: i.quantity + quantity } : i));
    else setItems([...items, { productId: product.id, quantity, productName: product.name, unitPrice: product.price }]);
    setSelectedProduct(''); setQuantity(1);
  };

  const handleSubmit = async () => {
    if (!customerName || items.length === 0) { setError('Nhập tên khách và chọn ít nhất 1 sản phẩm!'); return; }
    setLoading(true); setError('');
    try {
      const res = await api.post('/invoices', {
        customerName,
        currencyCode: currency,
        items: items.map(i => ({ productId: i.productId, quantity: i.quantity }))
      });
      // Gắn lại items từ form vào response để modal in có đủ thông tin
      const invoiceWithItems = {
        ...res.data,
        items: res.data.items ?? items.map(i => ({
          product: { name: i.productName },
          quantity: i.quantity,
          unitPrice: i.unitPrice,
        }))
      };
      setPrintInvoice(invoiceWithItems);
      setCustomerName(''); setItems([]);
    } catch (err: any) { setError(err.response?.data?.error || 'Lỗi tạo hóa đơn!'); }
    finally { setLoading(false); }
  };

  return (
    <div className="space-y-6 max-w-3xl">
      <div>
        <h1 className="text-2xl font-bold text-slate-800">Tạo Hóa Đơn Mới</h1>
        <p className="text-slate-400 text-sm mt-1">Chốt đơn nhanh như chớp. Tồn kho tự trừ, mã HD tự sinh.</p>
      </div>

      {error && <div className="bg-red-50 border border-red-200 text-red-600 p-4 rounded-2xl text-sm">{error}</div>}

      <div className="bg-white rounded-2xl border border-slate-100 shadow-sm p-6 space-y-5">
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label className="text-sm font-bold text-slate-600 block mb-2">Tên khách hàng</label>
            <input 
              list="customer-list"
              type="text" 
              value={customerName} 
              onChange={e => setCustomerName(e.target.value)}
              placeholder="Chọn hoặc nhập tên khách hàng..."
              className="w-full border border-slate-200 rounded-xl py-3 px-4 focus:outline-none focus:ring-2 focus:ring-indigo-300"
            />
            <datalist id="customer-list">
              {customers.map(c => (
                <option key={c.id} value={c.name}>{c.phone ? `SĐT: ${c.phone}` : ''}</option>
              ))}
            </datalist>
          </div>
          <div>
            <label className="text-sm font-bold text-slate-600 block mb-2">Tiền tệ</label>
            <select value={currency} onChange={e => setCurrency(e.target.value)}
              className="w-full border border-slate-200 rounded-xl py-3 px-4 bg-white cursor-pointer focus:outline-none focus:ring-2 focus:ring-indigo-300">
              <option value="VND">VND (Việt Nam Đồng)</option>
              <option value="USD">USD (Đô la Mỹ)</option>
              <option value="EUR">EUR (Euro)</option>
              <option value="JPY">JPY (Yên Nhật)</option>
            </select>
          </div>
        </div>

        <div>
          <label className="text-sm font-bold text-slate-600 block mb-2">Thêm sản phẩm</label>
          <div className="flex gap-3">
            <select value={selectedProduct} onChange={e => setSelectedProduct(e.target.value)}
              className="flex-1 border border-slate-200 rounded-xl py-3 px-4 bg-white cursor-pointer focus:outline-none focus:ring-2 focus:ring-indigo-300">
              <option value="">-- Chọn sản phẩm --</option>
              {products.map(p => <option key={p.id} value={p.id}>{p.name} (Còn: {p.stock} | {fmt(p.price)})</option>)}
            </select>
            <input type="number" min={1} value={quantity} onChange={e => setQuantity(Number(e.target.value))}
              className="w-24 border border-slate-200 rounded-xl py-3 px-4 text-center focus:outline-none focus:ring-2 focus:ring-indigo-300"/>
            <button onClick={addItem} disabled={!selectedProduct}
              className="flex items-center gap-2 bg-indigo-600 hover:bg-indigo-700 text-white font-bold py-3 px-5 rounded-xl transition disabled:opacity-50 cursor-pointer">
              <Plus size={18}/>Thêm
            </button>
          </div>
        </div>

        {items.length > 0 && (
          <div className="border border-slate-100 rounded-xl overflow-hidden">
            <table className="w-full">
              <thead className="bg-slate-50">
                <tr>
                  {['Sản phẩm','SL','Đơn giá','Thành tiền',''].map(h =>
                    <th key={h} className="text-xs font-bold text-slate-500 uppercase px-4 py-3 text-left last:text-center">{h}</th>)}
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-50">
                {items.map((item, i) => (
                  <tr key={i} className="hover:bg-slate-50">
                    <td className="px-4 py-3 font-medium text-slate-800">{item.productName}</td>
                    <td className="px-4 py-3 text-slate-600">{item.quantity}</td>
                    <td className="px-4 py-3 text-slate-600">{fmt(item.unitPrice)}</td>
                    <td className="px-4 py-3 font-bold text-indigo-600">{fmt(item.quantity * item.unitPrice)}</td>
                    <td className="px-4 py-3 text-center">
                      <button onClick={() => setItems(items.filter((_,j) => j!==i))} className="text-red-400 hover:text-red-600 cursor-pointer">
                        <Trash2 size={15}/>
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
              <tfoot className="bg-indigo-50">
                <tr>
                  <td colSpan={3} className="px-4 py-3 text-right font-bold text-slate-700">Tổng Cộng:</td>
                  <td className="px-4 py-3 font-extrabold text-indigo-700 text-lg">{fmt(total)}</td>
                  <td></td>
                </tr>
              </tfoot>
            </table>
          </div>
        )}

        <button onClick={handleSubmit} disabled={loading || items.length === 0}
          className="w-full flex items-center justify-center gap-3 bg-indigo-600 hover:bg-indigo-700 text-white font-extrabold py-4 rounded-2xl shadow-lg shadow-indigo-200 hover:-translate-y-0.5 transition-all disabled:opacity-60 disabled:hover:translate-y-0 cursor-pointer text-lg">
          <ShoppingBag size={22}/>{loading ? 'Đang chốt đơn...' : 'Chốt Đơn Ngay!'}
        </button>
      </div>

      {/* ── Print Invoice Modal ────────────────────────────── */}
      {printInvoice && (
        <div className="fixed inset-0 bg-black/50 backdrop-blur-sm flex items-center justify-center z-50 p-6">

          {/* Nút điều khiển — ẩn khi in */}
          <div className="print:hidden absolute top-6 right-6 flex gap-2">
            <button onClick={() => window.print()}
              className="flex items-center gap-2 bg-indigo-600 hover:bg-indigo-700 text-white font-bold px-5 py-2.5 rounded-xl shadow-lg cursor-pointer transition hover:-translate-y-0.5">
              <Printer size={17}/> In Hóa Đơn
            </button>
            <button onClick={() => setPrintInvoice(null)}
              className="flex items-center gap-2 bg-white text-slate-600 border border-slate-200 hover:bg-slate-50 font-semibold px-4 py-2.5 rounded-xl cursor-pointer transition">
              <X size={17}/> Đóng
            </button>
          </div>

          {/* Khung hóa đơn — đây là phần duy nhất được in */}
          <div id="invoice-print" className="bg-white rounded-2xl p-8 w-full max-w-md shadow-2xl">

            {/* Header */}
            <div className="text-center mb-6 pb-6 border-b-2 border-dashed border-slate-200">
              <p className="text-2xl font-extrabold text-indigo-700 tracking-tight">ERP.Vibe</p>
              <p className="text-slate-400 text-xs uppercase tracking-widest mt-0.5">Hóa đơn bán hàng</p>
              <p className="font-mono font-extrabold text-2xl text-slate-800 mt-2">{printInvoice.invoiceNumber}</p>
            </div>

            {/* Thông tin */}
            <div className="flex justify-between text-sm mb-5 text-slate-600">
              <div>
                <p className="text-xs text-slate-400 uppercase">Khách hàng</p>
                <p className="font-bold text-slate-800">{printInvoice.customerName}</p>
              </div>
              <div className="text-right">
                <p className="text-xs text-slate-400 uppercase">Ngày</p>
                <p className="font-medium">{new Date(printInvoice.invoiceDate).toLocaleDateString('vi-VN')}</p>
              </div>
            </div>

            {/* Bảng items */}
            <table className="w-full text-sm mb-5">
              <thead>
                <tr className="border-b-2 border-slate-200">
                  <th className="text-left py-2 text-slate-500 font-semibold">Sản phẩm</th>
                  <th className="text-center py-2 text-slate-500 font-semibold">SL</th>
                  <th className="text-right py-2 text-slate-500 font-semibold">Đơn giá</th>
                  <th className="text-right py-2 text-slate-500 font-semibold">T.Tiền</th>
                </tr>
              </thead>
              <tbody>
                {printInvoice.items?.map((item: any, idx: number) => (
                  <tr key={idx} className="border-b border-slate-100">
                    <td className="py-2">{item.product?.name ?? item.productName}</td>
                    <td className="text-center py-2">{item.quantity}</td>
                    <td className="text-right py-2">{fmt(item.unitPrice)}</td>
                    <td className="text-right py-2 font-semibold">{fmt(item.quantity * item.unitPrice)}</td>
                  </tr>
                ))}
              </tbody>
            </table>

            {/* Tổng */}
            <div className="border-t-2 border-slate-800 pt-3 flex justify-between items-center">
              <span className="font-bold text-slate-700">TỔNG CỘNG</span>
              <span className="font-extrabold text-2xl text-indigo-700">
                {new Intl.NumberFormat(printInvoice.currencyCode === 'VND' ? 'vi-VN' : 'en-US', {
                  style: 'currency',
                  currency: printInvoice.currencyCode
                }).format(printInvoice.totalAmount)}
              </span>
            </div>

            {/* Footer */}
            <div className="text-center mt-8 pt-6 border-t border-dashed border-slate-200">
              <p className="text-slate-400 text-sm">Cảm ơn quý khách! Hẹn gặp lại 🙏</p>
              <p className="text-xs text-slate-300 mt-1">Powered by ERP.Vibe</p>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
