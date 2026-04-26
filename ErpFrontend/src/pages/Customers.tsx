import React, { useState, useEffect, useRef } from 'react';
import axios from '../api/axios';
import { 
  UserPlus, 
  Search, 
  Edit2, 
  Trash2,
  X,
  Save,
  FileDown,
  Upload,
  Phone,
  Mail,
  MapPin,
  Building2,
  Hash
} from 'lucide-react';

interface Customer {
  id?: number;
  customerCode: string;
  name: string;
  phone: string;
  email: string;
  address: string;
  taxId: string;
}

export const Customers: React.FC = () => {
  const [customers, setCustomers] = useState<Customer[]>([]);
  const [loading, setLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState('');
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [currentCustomer, setCurrentCustomer] = useState<Customer | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    fetchCustomers();
  }, []);

  const fetchCustomers = async () => {
    try {
      const response = await axios.get('/customers');
      setCustomers(response.data);
    } catch (error) {
      console.error('Lỗi khi tải danh sách khách hàng:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!currentCustomer) return;

    try {
      if (currentCustomer.id) {
        await axios.put(`/customers/${currentCustomer.id}`, currentCustomer);
      } else {
        await axios.post('/customers', currentCustomer);
      }
      setIsModalOpen(false);
      fetchCustomers();
    } catch (error: any) {
      console.error('Lỗi khi lưu khách hàng:', error);
      alert(error.response?.data?.error || 'Không thể lưu khách hàng. Vui lòng kiểm tra lại!');
    }
  };

  const handleDelete = async (id: number) => {
    if (!confirm('Bạn có chắc chắn muốn xóa khách hàng này?')) return;
    try {
      await axios.delete(`/customers/${id}`);
      fetchCustomers();
    } catch (error) {
      console.error('Lỗi khi xóa khách hàng:', error);
    }
  };

  const handleExport = async () => {
    try {
      const response = await axios.get('/customers/export', { responseType: 'blob' });
      const url = window.URL.createObjectURL(new Blob([response.data]));
      const link = document.createElement('a');
      link.href = url;
      link.setAttribute('download', 'Customers.xlsx');
      document.body.appendChild(link);
      link.click();
      link.remove();
    } catch (error) {
      console.error('Lỗi khi xuất Excel:', error);
    }
  };

  const handleImport = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    const formData = new FormData();
    formData.append('file', file);

    try {
      setLoading(true);
      const res = await axios.post('/customers/import', formData, {
        headers: { 'Content-Type': 'multipart/form-data' }
      });
      alert(`Nhập dữ liệu thành công! Thêm mới: ${res.data.count}, Bỏ qua trùng: ${res.data.skipped}`);
      fetchCustomers();
    } catch (error) {
      console.error('Lỗi khi nhập Excel:', error);
      alert('Lỗi khi nhập file Excel. Vui lòng kiểm tra định dạng!');
    } finally {
      setLoading(false);
      if (fileInputRef.current) fileInputRef.current.value = '';
    }
  };

  const filteredCustomers = customers.filter(c => 
    c.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
    c.phone?.includes(searchTerm) ||
    c.customerCode?.toLowerCase().includes(searchTerm.toLowerCase())
  );

  return (
    <div className="p-6">
      <div className="flex flex-col md:flex-row md:items-center justify-between mb-8 gap-4">
        <div>
          <h1 className="text-3xl font-bold text-slate-800 flex items-center gap-3">
            <Building2 className="w-8 h-8 text-indigo-600" />
            Quản lý Đối tác
          </h1>
          <p className="text-slate-500 mt-1">Quản lý danh sách khách hàng và nhà cung cấp chuyên nghiệp</p>
        </div>

        <div className="flex gap-3">
          <input 
            type="file" 
            ref={fileInputRef} 
            onChange={handleImport} 
            className="hidden" 
            accept=".xlsx, .xls" 
          />
          <button
            onClick={() => fileInputRef.current?.click()}
            className="flex items-center gap-2 bg-emerald-50 text-emerald-700 border border-emerald-100 hover:bg-emerald-100 px-4 py-2.5 rounded-xl transition-all font-semibold"
          >
            <Upload className="w-4 h-4" />
            Nhập Excel
          </button>
          <button
            onClick={handleExport}
            className="flex items-center gap-2 bg-blue-50 text-blue-700 border border-blue-100 hover:bg-blue-100 px-4 py-2.5 rounded-xl transition-all font-semibold"
          >
            <FileDown className="w-4 h-4" />
            Xuất Excel
          </button>
          <button
            onClick={() => {
              setCurrentCustomer({ customerCode: '', name: '', phone: '', email: '', address: '', taxId: '' });
              setIsModalOpen(true);
            }}
            className="flex items-center gap-2 bg-indigo-600 hover:bg-indigo-700 text-white px-6 py-2.5 rounded-xl transition-all shadow-lg shadow-indigo-200 font-semibold"
          >
            <UserPlus className="w-5 h-5" />
            Thêm mới
          </button>
        </div>
      </div>

      {/* Thanh tìm kiếm */}
      <div className="bg-white p-4 rounded-2xl shadow-sm border border-slate-100 mb-6">
        <div className="relative">
          <Search className="absolute left-4 top-1/2 -translate-y-1/2 text-slate-400 w-5 h-5" />
          <input
            type="text"
            placeholder="Tìm kiếm theo mã, tên hoặc số điện thoại..."
            className="w-full pl-12 pr-4 py-3 bg-slate-50 border-none rounded-xl focus:ring-2 focus:ring-indigo-500 transition-all"
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
          />
        </div>
      </div>

      <div className="bg-white rounded-2xl shadow-sm border border-slate-100 overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-left border-collapse">
            <thead>
              <tr className="bg-slate-50 border-b border-slate-100">
                <th className="px-6 py-4 text-xs font-bold text-slate-500 uppercase tracking-wider">Mã KH</th>
                <th className="px-6 py-4 text-xs font-bold text-slate-500 uppercase tracking-wider">Tên khách hàng</th>
                <th className="px-6 py-4 text-xs font-bold text-slate-500 uppercase tracking-wider">Liên hệ</th>
                <th className="px-6 py-4 text-xs font-bold text-slate-500 uppercase tracking-wider">Địa chỉ</th>
                <th className="px-6 py-4 text-xs font-bold text-slate-500 uppercase tracking-wider text-right">Thao tác</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {loading ? (
                <tr>
                  <td colSpan={5} className="px-6 py-12 text-center text-slate-400 font-medium">Đang xử lý dữ liệu...</td>
                </tr>
              ) : filteredCustomers.length === 0 ? (
                <tr>
                  <td colSpan={5} className="px-6 py-12 text-center text-slate-400">Không tìm thấy khách hàng nào</td>
                </tr>
              ) : (
                filteredCustomers.map((customer) => (
                  <tr key={customer.id} className="hover:bg-slate-50/50 transition-colors group">
                    <td className="px-6 py-4">
                      <span className="font-mono text-xs font-bold bg-slate-100 text-slate-600 px-2 py-1 rounded">
                        {customer.customerCode}
                      </span>
                    </td>
                    <td className="px-6 py-4">
                      <div className="flex items-center gap-3">
                        <div className="w-10 h-10 rounded-full bg-indigo-50 flex items-center justify-center text-indigo-600 font-bold shadow-sm border border-indigo-100">
                          {customer.name.charAt(0)}
                        </div>
                        <span className="font-bold text-slate-800">{customer.name}</span>
                      </div>
                    </td>
                    <td className="px-6 py-4">
                      <div className="space-y-1">
                        <div className="flex items-center gap-2 text-sm text-slate-700 font-medium">
                          <Phone className="w-3.5 h-3.5 text-indigo-500" />
                          {customer.phone || '---'}
                        </div>
                        <div className="flex items-center gap-2 text-xs text-slate-400">
                          <Mail className="w-3.5 h-3.5" />
                          {customer.email || '---'}
                        </div>
                      </div>
                    </td>
                    <td className="px-6 py-4">
                      <div className="flex items-start gap-2 text-sm text-slate-600 max-w-[200px]">
                        <MapPin className="w-3.5 h-3.5 mt-0.5 shrink-0 text-slate-400" />
                        <span className="truncate">{customer.address || '---'}</span>
                      </div>
                    </td>
                    <td className="px-6 py-4 text-right">
                      <div className="flex justify-end gap-2">
                        <button 
                          onClick={() => {
                            setCurrentCustomer(customer);
                            setIsModalOpen(true);
                          }}
                          className="p-2 text-slate-400 hover:text-indigo-600 hover:bg-indigo-50 rounded-lg transition-all"
                          title="Chỉnh sửa"
                        >
                          <Edit2 className="w-4 h-4" />
                        </button>
                        <button 
                          onClick={() => customer.id && handleDelete(customer.id)}
                          className="p-2 text-slate-400 hover:text-red-600 hover:bg-red-50 rounded-lg transition-all"
                          title="Xóa"
                        >
                          <Trash2 className="w-4 h-4" />
                        </button>
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* Modal Thêm/Sửa */}
      {isModalOpen && (
        <div className="fixed inset-0 bg-slate-900/60 backdrop-blur-sm flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-3xl w-full max-w-lg shadow-2xl overflow-hidden animate-in fade-in zoom-in duration-200">
            <div className="p-6 border-b border-slate-100 flex justify-between items-center bg-slate-50">
              <h2 className="text-xl font-bold text-slate-800">
                {currentCustomer?.id ? 'Cập nhật thông tin' : 'Thêm khách hàng mới'}
              </h2>
              <button 
                onClick={() => setIsModalOpen(false)}
                className="p-2 hover:bg-white rounded-full transition-all"
              >
                <X className="w-5 h-5 text-slate-500" />
              </button>
            </div>

            <form onSubmit={handleSave} className="p-6 space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-semibold text-slate-700 mb-1 flex items-center gap-2">
                    <Hash className="w-4 h-4 text-slate-400" /> Mã khách hàng
                  </label>
                  <input
                    type="text"
                    placeholder="Tự động sinh nếu để trống"
                    className="w-full px-4 py-3 bg-slate-50 border-none rounded-xl focus:ring-2 focus:ring-indigo-500 text-sm font-mono"
                    value={currentCustomer?.customerCode}
                    onChange={(e) => setCurrentCustomer(prev => ({ ...prev!, customerCode: e.target.value }))}
                  />
                </div>
                <div>
                  <label className="block text-sm font-semibold text-slate-700 mb-1">Mã số thuế</label>
                  <input
                    type="text"
                    className="w-full px-4 py-3 bg-slate-50 border-none rounded-xl focus:ring-2 focus:ring-indigo-500 text-sm"
                    value={currentCustomer?.taxId}
                    onChange={(e) => setCurrentCustomer(prev => ({ ...prev!, taxId: e.target.value }))}
                  />
                </div>
              </div>

              <div>
                <label className="block text-sm font-semibold text-slate-700 mb-1">Tên khách hàng *</label>
                <input
                  required
                  type="text"
                  className="w-full px-4 py-3 bg-slate-50 border-none rounded-xl focus:ring-2 focus:ring-indigo-500"
                  value={currentCustomer?.name}
                  onChange={(e) => setCurrentCustomer(prev => ({ ...prev!, name: e.target.value }))}
                />
              </div>

              <div>
                <label className="block text-sm font-semibold text-slate-700 mb-1 flex items-center gap-2">
                  <Phone className="w-4 h-4 text-slate-400" /> Số điện thoại *
                </label>
                <input
                  required
                  type="text"
                  placeholder="Bắt buộc và không được trùng"
                  className="w-full px-4 py-3 bg-slate-50 border-none rounded-xl focus:ring-2 focus:ring-indigo-500"
                  value={currentCustomer?.phone}
                  onChange={(e) => setCurrentCustomer(prev => ({ ...prev!, phone: e.target.value }))}
                />
              </div>

              <div>
                <label className="block text-sm font-semibold text-slate-700 mb-1 flex items-center gap-2">
                  <Mail className="w-4 h-4 text-slate-400" /> Email
                </label>
                <input
                  type="email"
                  className="w-full px-4 py-3 bg-slate-50 border-none rounded-xl focus:ring-2 focus:ring-indigo-500"
                  value={currentCustomer?.email}
                  onChange={(e) => setCurrentCustomer(prev => ({ ...prev!, email: e.target.value }))}
                />
              </div>

              <div>
                <label className="block text-sm font-semibold text-slate-700 mb-1 flex items-center gap-2">
                  <MapPin className="w-4 h-4 text-slate-400" /> Địa chỉ
                </label>
                <textarea
                  rows={2}
                  className="w-full px-4 py-3 bg-slate-50 border-none rounded-xl focus:ring-2 focus:ring-indigo-500 text-sm"
                  value={currentCustomer?.address}
                  onChange={(e) => setCurrentCustomer(prev => ({ ...prev!, address: e.target.value }))}
                />
              </div>

              <div className="flex gap-3 pt-4">
                <button
                  type="button"
                  onClick={() => setIsModalOpen(false)}
                  className="flex-1 px-6 py-3 border border-slate-200 text-slate-600 font-semibold rounded-xl hover:bg-slate-50 transition-all"
                >
                  Hủy
                </button>
                <button
                  type="submit"
                  className="flex-1 flex items-center justify-center gap-2 bg-indigo-600 hover:bg-indigo-700 text-white font-semibold rounded-xl transition-all shadow-lg shadow-indigo-200"
                >
                  <Save className="w-5 h-5" />
                  Lưu khách hàng
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};
