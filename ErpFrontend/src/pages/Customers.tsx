import React, { useState, useEffect, useRef } from 'react';
import axios from '../api/axios';
import { useAuth } from '../context/AuthContext';
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
  Hash,
  Globe
} from 'lucide-react';

interface Customer {
  id?: number;
  customerCode?: string;
  name: string;
  phone: string;
  email: string;
  address: string;
  taxId: string;
  customerBranches?: { branchId: number; branch?: { name: string } }[];
  branchIds?: number[];
  totalDebt?: number;
}

interface CustomerHistory {
  customerName: string;
  totalDebt: number;
  debtByBranch?: {
    branchName: string;
    debtAmount: number;
  }[];
  invoices: {
    id: number;
    invoiceNumber: string;
    invoiceDate: string;
    totalAmount: number;
    paidAmount: number;
    status: string;
    currencyCode: string;
    branchName: string;
  }[];
  payments: {
    id: number;
    invoiceNumber: string;
    amount: number;
    paymentDate: string;
    paymentMethod: string;
    processedBy: string;
  }[];
}

interface Branch {
  id: number;
  name: string;
}

export const Customers: React.FC = () => {
  const { role } = useAuth();
  const [customers, setCustomers] = useState<Customer[]>([]);
  const [branches, setBranches] = useState<Branch[]>([]);
  const [loading, setLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState('');
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [currentCustomer, setCurrentCustomer] = useState<Customer | null>(null);
  
  // Debt Ledger State
  const [isLedgerModalOpen, setIsLedgerModalOpen] = useState(false);
  const [ledgerData, setLedgerData] = useState<CustomerHistory | null>(null);
  const [loadingLedger, setLoadingLedger] = useState(false);

  const fileInputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    fetchCustomers();
    if (role === 'Admin') fetchBranches();
  }, [role]);

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

  const fetchBranches = async () => {
    try {
      const response = await axios.get('/branches');
      setBranches(response.data);
    } catch (error) {
      console.error('Lỗi khi tải danh sách chi nhánh:', error);
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
      
      // Xử lý xác nhận trùng SĐT
      if (error.response?.status === 409 && error.response?.data?.error === 'DUPLICATE_PHONE') {
        if (confirm(error.response.data.message)) {
          try {
            await axios.post('/customers?confirmDuplicate=true', currentCustomer);
            setIsModalOpen(false);
            fetchCustomers();
            return;
          } catch (retryError: any) {
            alert(retryError.response?.data?.error || 'Không thể liên kết khách hàng. Vui lòng thử lại!');
            return;
          }
        } else {
          return; // Người dùng không đồng ý
        }
      }

      const serverError = error.response?.data?.error || error.response?.data?.detail;
      alert(serverError || 'Không thể lưu khách hàng. Vui lòng kiểm tra lại!');
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

  const toggleBranch = (branchId: number) => {
    if (!currentCustomer) return;
    const currentIds = currentCustomer.branchIds || [];
    const newIds = currentIds.includes(branchId)
      ? currentIds.filter(id => id !== branchId)
      : [...currentIds, branchId];
    setCurrentCustomer({ ...currentCustomer, branchIds: newIds });
  };

  const openLedger = async (customerId: number) => {
    setIsLedgerModalOpen(true);
    setLoadingLedger(true);
    setLedgerData(null);
    try {
      const response = await axios.get(`/customers/${customerId}/history`);
      setLedgerData(response.data);
    } catch (error) {
      console.error('Lỗi khi tải sổ công nợ:', error);
      alert('Không thể tải dữ liệu công nợ.');
      setIsLedgerModalOpen(false);
    } finally {
      setLoadingLedger(false);
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
            Partners Management
          </h1>
          <p className="text-slate-500 mt-1">Manage customers and suppliers across branches</p>
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
            Import
          </button>
          <button
            onClick={handleExport}
            className="flex items-center gap-2 bg-blue-50 text-blue-700 border border-blue-100 hover:bg-blue-100 px-4 py-2.5 rounded-xl transition-all font-semibold"
          >
            <FileDown className="w-4 h-4" />
            Export
          </button>
          <button
            onClick={() => {
              setCurrentCustomer({ customerCode: '', name: '', phone: '', email: '', address: '', taxId: '', branchIds: [] });
              setIsModalOpen(true);
            }}
            className="flex items-center gap-2 bg-indigo-600 hover:bg-indigo-700 text-white px-6 py-2.5 rounded-xl transition-all shadow-lg shadow-indigo-200 font-semibold"
          >
            <UserPlus className="w-5 h-5" />
            Add Customer
          </button>
        </div>
      </div>

      {/* Search Bar */}
      <div className="bg-white p-4 rounded-2xl shadow-sm border border-slate-100 mb-6">
        <div className="relative">
          <Search className="absolute left-4 top-1/2 -translate-y-1/2 text-slate-400 w-5 h-5" />
          <input
            type="text"
            placeholder="Search by code, name or phone..."
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
                <th className="px-6 py-4 text-xs font-bold text-slate-500 uppercase tracking-wider">Code</th>
                <th className="px-6 py-4 text-xs font-bold text-slate-500 uppercase tracking-wider">Name</th>
                <th className="px-6 py-4 text-xs font-bold text-slate-500 uppercase tracking-wider">Contact</th>
                <th className="px-6 py-4 text-xs font-bold text-slate-500 uppercase tracking-wider">Address</th>
                <th className="px-6 py-4 text-xs font-bold text-slate-500 uppercase tracking-wider text-right">Total Debt</th>
                {role === 'Admin' && <th className="px-6 py-4 text-xs font-bold text-slate-500 uppercase tracking-wider">Branches</th>}
                <th className="px-6 py-4 text-xs font-bold text-slate-500 uppercase tracking-wider text-right">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {loading ? (
                <tr>
                  <td colSpan={role === 'Admin' ? 7 : 6} className="px-6 py-12 text-center text-slate-400 font-medium">Loading data...</td>
                </tr>
              ) : filteredCustomers.length === 0 ? (
                <tr>
                  <td colSpan={role === 'Admin' ? 7 : 6} className="px-6 py-12 text-center text-slate-400">No customers found</td>
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
                      <span className={`font-bold ${customer.totalDebt && customer.totalDebt > 0 ? 'text-rose-600' : 'text-slate-500'}`}>
                        {customer.totalDebt ? customer.totalDebt.toLocaleString() + '₫' : '0₫'}
                      </span>
                    </td>
                    {role === 'Admin' && (
                      <td className="px-6 py-4">
                        <div className="flex flex-wrap gap-1">
                          {customer.customerBranches?.map(cb => (
                            <span key={cb.branchId} className="text-[10px] font-bold bg-indigo-50 text-indigo-600 px-2 py-0.5 rounded-full border border-indigo-100">
                              {cb.branch?.name}
                            </span>
                          )) || <span className="text-xs text-slate-300">None</span>}
                        </div>
                      </td>
                    )}
                    <td className="px-6 py-4 text-right">
                      <div className="flex justify-end gap-2">
                        <button 
                          onClick={() => openLedger(customer.id!)}
                          title="Sổ công nợ"
                          className="p-2 text-emerald-600 hover:bg-emerald-50 rounded-lg transition-colors border border-emerald-100 bg-white"
                        >
                          <Hash className="w-4 h-4" />
                        </button>
                        <button 
                          onClick={() => {
                            setCurrentCustomer({
                              ...customer,
                              branchIds: customer.customerBranches?.map(cb => cb.branchId) || []
                            });
                            setIsModalOpen(true);
                          }}
                          className="p-2 text-slate-400 hover:text-indigo-600 hover:bg-indigo-50 rounded-lg transition-all"
                          title="Edit"
                        >
                          <Edit2 className="w-4 h-4" />
                        </button>
                        <button 
                          onClick={() => customer.id && handleDelete(customer.id)}
                          className="p-2 text-slate-400 hover:text-red-600 hover:bg-red-50 rounded-lg transition-all"
                          title="Delete"
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

      {/* Modal */}
      {isModalOpen && (
        <div className="fixed inset-0 bg-slate-900/60 backdrop-blur-sm flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-3xl w-full max-w-lg shadow-2xl overflow-hidden animate-in fade-in zoom-in duration-200">
            <div className="p-6 border-b border-slate-100 flex justify-between items-center bg-slate-50">
              <h2 className="text-xl font-bold text-slate-800">
                {currentCustomer?.id ? 'Edit Customer' : 'Add New Customer'}
              </h2>
              <button 
                onClick={() => setIsModalOpen(false)}
                className="p-2 hover:bg-white rounded-full transition-all"
              >
                <X className="w-5 h-5 text-slate-500" />
              </button>
            </div>

            <form onSubmit={handleSave} className="p-6 space-y-4 max-h-[70vh] overflow-y-auto">
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-semibold text-slate-700 mb-1 flex items-center gap-2">
                    <Hash className="w-4 h-4 text-slate-400" /> Code
                  </label>
                  <input
                    type="text"
                    placeholder="Auto-generated"
                    className="w-full px-4 py-3 bg-slate-50 border-none rounded-xl focus:ring-2 focus:ring-indigo-500 text-sm font-mono"
                    value={currentCustomer?.customerCode}
                    onChange={(e) => setCurrentCustomer(prev => ({ ...prev!, customerCode: e.target.value }))}
                  />
                </div>
                <div>
                  <label className="block text-sm font-semibold text-slate-700 mb-1">Tax ID</label>
                  <input
                    type="text"
                    className="w-full px-4 py-3 bg-slate-50 border-none rounded-xl focus:ring-2 focus:ring-indigo-500 text-sm"
                    value={currentCustomer?.taxId}
                    onChange={(e) => setCurrentCustomer(prev => ({ ...prev!, taxId: e.target.value }))}
                  />
                </div>
              </div>

              <div>
                <label className="block text-sm font-semibold text-slate-700 mb-1">Name *</label>
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
                  <Phone className="w-4 h-4 text-slate-400" /> Phone *
                </label>
                <input
                  required
                  type="text"
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
                  <MapPin className="w-4 h-4 text-slate-400" /> Address
                </label>
                <textarea
                  rows={2}
                  className="w-full px-4 py-3 bg-slate-50 border-none rounded-xl focus:ring-2 focus:ring-indigo-500 text-sm"
                  value={currentCustomer?.address}
                  onChange={(e) => setCurrentCustomer(prev => ({ ...prev!, address: e.target.value }))}
                />
              </div>

              {role === 'Admin' && (
                <div>
                  <label className="block text-sm font-semibold text-slate-700 mb-2 flex items-center gap-2">
                    <Globe className="w-4 h-4 text-indigo-500" /> Branch Assignments
                  </label>
                  <div className="grid grid-cols-2 gap-2 bg-slate-50 p-4 rounded-2xl">
                    {branches.map(branch => (
                      <label key={branch.id} className="flex items-center gap-2 p-2 bg-white rounded-lg border border-slate-100 cursor-pointer hover:border-indigo-200 transition-colors">
                        <input
                          type="checkbox"
                          className="w-4 h-4 rounded text-indigo-600 focus:ring-indigo-500 border-slate-300"
                          checked={currentCustomer?.branchIds?.includes(branch.id)}
                          onChange={() => toggleBranch(branch.id)}
                        />
                        <span className="text-sm font-medium text-slate-700">{branch.name}</span>
                      </label>
                    ))}
                  </div>
                </div>
              )}

              <div className="flex gap-3 pt-4">
                <button
                  type="button"
                  onClick={() => setIsModalOpen(false)}
                  className="flex-1 px-6 py-3 border border-slate-200 text-slate-600 font-semibold rounded-xl hover:bg-slate-50 transition-all"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  className="flex-1 flex items-center justify-center gap-2 bg-indigo-600 hover:bg-indigo-700 text-white font-semibold rounded-xl transition-all shadow-lg shadow-indigo-200"
                >
                  <Save className="w-5 h-5" />
                  Save Changes
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Debt Ledger Modal */}
      {isLedgerModalOpen && (
        <div className="fixed inset-0 bg-slate-900/50 backdrop-blur-sm flex items-center justify-center p-4 z-50">
          <div className="bg-white rounded-3xl w-full max-w-4xl max-h-[90vh] overflow-hidden flex flex-col shadow-2xl">
            <div className="px-6 py-4 border-b border-slate-100 flex items-center justify-between bg-slate-50/50">
              <div className="flex items-center gap-3">
                <div className="w-10 h-10 rounded-xl bg-emerald-100 flex items-center justify-center text-emerald-600">
                  <Hash className="w-5 h-5" />
                </div>
                <div>
                  <h3 className="text-xl font-bold text-slate-800">
                    Sổ công nợ: {ledgerData?.customerName || '...'}
                  </h3>
                  <p className="text-sm text-slate-500">Chi tiết mua hàng và thanh toán</p>
                </div>
              </div>
              <button onClick={() => setIsLedgerModalOpen(false)} className="text-slate-400 hover:text-slate-600 bg-white hover:bg-slate-100 p-2 rounded-full transition-colors shadow-sm">
                <X className="w-5 h-5" />
              </button>
            </div>

            <div className="p-6 overflow-y-auto bg-slate-50/30 flex-1">
              {loadingLedger ? (
                <div className="text-center py-12 text-slate-400 font-medium">Đang tải dữ liệu...</div>
              ) : ledgerData ? (
                <div className="space-y-6">
                  {/* Summary Card */}
                  <div className="bg-white p-6 rounded-2xl shadow-sm border border-slate-100 flex flex-col md:flex-row md:items-center justify-between gap-4">
                    <div>
                      <p className="text-sm font-semibold text-slate-500 uppercase tracking-wider mb-1">Tổng nợ hiện tại</p>
                      <h4 className={`text-3xl font-bold ${ledgerData.totalDebt > 0 ? 'text-rose-600' : 'text-emerald-600'}`}>
                        {ledgerData.totalDebt.toLocaleString()}₫
                      </h4>
                    </div>
                    
                    {/* Breakdown by Branch */}
                    {ledgerData.debtByBranch && ledgerData.debtByBranch.length > 0 && (
                      <div className="flex flex-wrap gap-2">
                        {ledgerData.debtByBranch.map(d => (
                          <div key={d.branchName} className="bg-rose-50 border border-rose-100 px-4 py-2 rounded-xl text-right">
                            <p className="text-xs font-bold text-rose-800/60 uppercase">{d.branchName}</p>
                            <p className="font-bold text-rose-600">{d.debtAmount.toLocaleString()}₫</p>
                          </div>
                        ))}
                      </div>
                    )}
                  </div>

                  <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                    {/* Invoices */}
                    <div className="bg-white rounded-2xl shadow-sm border border-slate-100 overflow-hidden">
                      <div className="px-5 py-4 border-b border-slate-100 bg-slate-50/50">
                        <h5 className="font-bold text-slate-800">Lịch sử Hóa đơn</h5>
                      </div>
                      <div className="divide-y divide-slate-100 max-h-[400px] overflow-y-auto">
                        {ledgerData.invoices.length === 0 ? (
                          <div className="p-5 text-center text-slate-400">Chưa có hóa đơn nào</div>
                        ) : ledgerData.invoices.map(inv => (
                          <div key={inv.id} className="p-4 hover:bg-slate-50 transition-colors">
                            <div className="flex justify-between items-start mb-2">
                              <div>
                                <span className="font-mono text-sm font-bold text-slate-700">{inv.invoiceNumber}</span>
                                <span className="ml-2 text-xs font-bold bg-indigo-50 text-indigo-600 px-2 py-0.5 rounded-full border border-indigo-100">{inv.branchName}</span>
                              </div>
                              <span className="text-xs text-slate-400">{new Date(inv.invoiceDate).toLocaleDateString('vi-VN')}</span>
                            </div>
                            <div className="flex justify-between text-sm">
                              <span className="text-slate-500">Tổng: <span className="font-bold text-slate-800">{inv.totalAmount.toLocaleString()}₫</span></span>
                              <span className="text-slate-500">Đã trả: <span className="font-bold text-emerald-600">{inv.paidAmount.toLocaleString()}₫</span></span>
                            </div>
                            {inv.totalAmount > inv.paidAmount && (
                              <div className="mt-2 text-right text-xs font-bold text-rose-500">
                                Còn nợ: {(inv.totalAmount - inv.paidAmount).toLocaleString()}₫
                              </div>
                            )}
                          </div>
                        ))}
                      </div>
                    </div>

                    {/* Payments */}
                    <div className="bg-white rounded-2xl shadow-sm border border-slate-100 overflow-hidden">
                      <div className="px-5 py-4 border-b border-slate-100 bg-slate-50/50">
                        <h5 className="font-bold text-slate-800">Lịch sử Thanh toán</h5>
                      </div>
                      <div className="divide-y divide-slate-100 max-h-[400px] overflow-y-auto">
                        {ledgerData.payments.length === 0 ? (
                          <div className="p-5 text-center text-slate-400">Chưa có thanh toán nào</div>
                        ) : ledgerData.payments.map(pay => (
                          <div key={pay.id} className="p-4 hover:bg-slate-50 transition-colors">
                            <div className="flex justify-between items-start mb-2">
                              <span className="font-mono text-sm font-bold text-slate-700">Thanh toán cho: {pay.invoiceNumber}</span>
                              <span className="text-xs text-slate-400">{new Date(pay.paymentDate).toLocaleDateString('vi-VN')}</span>
                            </div>
                            <div className="flex justify-between text-sm">
                              <span className="font-bold text-emerald-600">+{pay.amount.toLocaleString()}₫</span>
                              <span className="text-xs text-slate-500 bg-slate-100 px-2 py-1 rounded">{pay.paymentMethod}</span>
                            </div>
                          </div>
                        ))}
                      </div>
                    </div>
                  </div>
                </div>
              ) : null}
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

