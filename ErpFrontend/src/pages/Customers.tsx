import React, { useState, useEffect } from 'react';
import axios from '../api/axios';
import { 
  Users, 
  UserPlus, 
  Search, 
  Mail, 
  Phone, 
  MapPin, 
  Edit2, 
  Trash2,
  X,
  Save
} from 'lucide-react';

interface Customer {
  id?: number;
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

  useEffect(() => {
    fetchCustomers();
  }, []);

  const fetchCustomers = async () => {
    try {
      const response = await axios.get('/api/customers');
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
        await axios.put(`/api/customers/${currentCustomer.id}`, currentCustomer);
      } else {
        await axios.post('/api/customers', currentCustomer);
      }
      setIsModalOpen(false);
      fetchCustomers();
    } catch (error) {
      console.error('Lỗi khi lưu khách hàng:', error);
      alert('Không thể lưu khách hàng. Vui lòng kiểm tra lại!');
    }
  };

  const handleDelete = async (id: number) => {
    if (!confirm('Bạn có chắc chắn muốn xóa khách hàng này?')) return;
    try {
      await axios.delete(`/api/customers/${id}`);
      fetchCustomers();
    } catch (error) {
      console.error('Lỗi khi xóa khách hàng:', error);
    }
  };

  const filteredCustomers = customers.filter(c => 
    c.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
    c.phone.includes(searchTerm)
  );

  return (
    <div className="p-6">
      <div className="flex flex-col md:flex-row md:items-center justify-between mb-8 gap-4">
        <div>
          <h1 className="text-3xl font-bold text-slate-800 flex items-center gap-3">
            <Users className="w-8 h-8 text-indigo-600" />
            Quản lý Khách hàng
          </h1>
          <p className="text-slate-500 mt-1">Danh sách đối tác và khách hàng của chi nhánh</p>
        </div>

        <button
          onClick={() => {
            setCurrentCustomer({ name: '', phone: '', email: '', address: '', taxId: '' });
            setIsModalOpen(true);
          }}
          className="flex items-center gap-2 bg-indigo-600 hover:bg-indigo-700 text-white px-6 py-3 rounded-xl transition-all shadow-lg shadow-indigo-200"
        >
          <UserPlus className="w-5 h-5" />
          <span className="font-semibold">Thêm khách hàng</span>
        </button>
      </div>

      {/* Thanh tìm kiếm */}
      <div className="bg-white p-4 rounded-2xl shadow-sm border border-slate-100 mb-6">
        <div className="relative">
          <Search className="absolute left-4 top-1/2 -translate-y-1/2 text-slate-400 w-5 h-5" />
          <input
            type="text"
            placeholder="Tìm kiếm theo tên hoặc số điện thoại..."
            className="w-full pl-12 pr-4 py-3 bg-slate-50 border-none rounded-xl focus:ring-2 focus:ring-indigo-500 transition-all"
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
          />
        </div>
      </div>

      {loading ? (
        <div className="flex justify-center p-12">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-indigo-600"></div>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {filteredCustomers.map((customer) => (
            <div 
              key={customer.id}
              className="bg-white rounded-2xl p-6 shadow-sm border border-slate-100 hover:shadow-md transition-all group"
            >
              <div className="flex justify-between items-start mb-4">
                <div className="bg-indigo-50 p-3 rounded-xl">
                  <Users className="w-6 h-6 text-indigo-600" />
                </div>
                <div className="flex gap-2 opacity-0 group-hover:opacity-100 transition-all">
                  <button 
                    onClick={() => {
                      setCurrentCustomer(customer);
                      setIsModalOpen(true);
                    }}
                    className="p-2 text-slate-400 hover:text-indigo-600 hover:bg-indigo-50 rounded-lg"
                  >
                    <Edit2 className="w-4 h-4" />
                  </button>
                  <button 
                    onClick={() => customer.id && handleDelete(customer.id)}
                    className="p-2 text-slate-400 hover:text-red-600 hover:bg-red-50 rounded-lg"
                  >
                    <Trash2 className="w-4 h-4" />
                  </button>
                </div>
              </div>

              <h3 className="text-xl font-bold text-slate-800 mb-4">{customer.name}</h3>

              <div className="space-y-3">
                <div className="flex items-center gap-3 text-slate-600">
                  <Phone className="w-4 h-4" />
                  <span className="text-sm">{customer.phone || 'Chưa cập nhật'}</span>
                </div>
                <div className="flex items-center gap-3 text-slate-600">
                  <Mail className="w-4 h-4" />
                  <span className="text-sm truncate">{customer.email || 'Chưa cập nhật'}</span>
                </div>
                <div className="flex items-start gap-3 text-slate-600">
                  <MapPin className="w-4 h-4 mt-1" />
                  <span className="text-sm line-clamp-2">{customer.address || 'Chưa cập nhật'}</span>
                </div>
              </div>
              
              {customer.taxId && (
                <div className="mt-4 pt-4 border-t border-slate-50">
                  <span className="text-xs font-semibold text-slate-400 uppercase tracking-wider">Mã số thuế:</span>
                  <p className="text-sm font-medium text-slate-700">{customer.taxId}</p>
                </div>
              )}
            </div>
          ))}
        </div>
      )}

      {/* Modal Thêm/Sửa */}
      {isModalOpen && (
        <div className="fixed inset-0 bg-slate-900/50 backdrop-blur-sm flex items-center justify-center z-50 p-4">
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

              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-semibold text-slate-700 mb-1">Số điện thoại</label>
                  <input
                    type="text"
                    className="w-full px-4 py-3 bg-slate-50 border-none rounded-xl focus:ring-2 focus:ring-indigo-500"
                    value={currentCustomer?.phone}
                    onChange={(e) => setCurrentCustomer(prev => ({ ...prev!, phone: e.target.value }))}
                  />
                </div>
                <div>
                  <label className="block text-sm font-semibold text-slate-700 mb-1">Mã số thuế</label>
                  <input
                    type="text"
                    className="w-full px-4 py-3 bg-slate-50 border-none rounded-xl focus:ring-2 focus:ring-indigo-500"
                    value={currentCustomer?.taxId}
                    onChange={(e) => setCurrentCustomer(prev => ({ ...prev!, taxId: e.target.value }))}
                  />
                </div>
              </div>

              <div>
                <label className="block text-sm font-semibold text-slate-700 mb-1">Email</label>
                <input
                  type="email"
                  className="w-full px-4 py-3 bg-slate-50 border-none rounded-xl focus:ring-2 focus:ring-indigo-500"
                  value={currentCustomer?.email}
                  onChange={(e) => setCurrentCustomer(prev => ({ ...prev!, email: e.target.value }))}
                />
              </div>

              <div>
                <label className="block text-sm font-semibold text-slate-700 mb-1">Địa chỉ</label>
                <textarea
                  rows={3}
                  className="w-full px-4 py-3 bg-slate-50 border-none rounded-xl focus:ring-2 focus:ring-indigo-500"
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
                  Lưu thông tin
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};
