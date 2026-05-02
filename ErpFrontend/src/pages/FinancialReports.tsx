import React, { useState, useEffect } from 'react';
import axios from 'axios';
import { Landmark, TrendingUp, TrendingDown, ReceiptText, PieChart, Activity } from 'lucide-react';

interface AccountBalance {
  id: number;
  code: string;
  name: string;
  typeName: string;
  totalDebit: number;
  totalCredit: number;
  balance: number;
}

interface JournalEntry {
  id: string;
  entryDate: string;
  reference: string;
  description: string;
  lines: {
    code: string;
    name: string;
    debit: number;
    credit: number;
  }[];
}

export const FinancialReports: React.FC = () => {
  const [balances, setBalances] = useState<AccountBalance[]>([]);
  const [entries, setEntries] = useState<JournalEntry[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetchData();
  }, []);

  const fetchData = async () => {
    try {
      const token = localStorage.getItem('token');
      const headers = { Authorization: `Bearer ${token}` };
      
      const [balRes, entryRes] = await Promise.all([
        axios.get('http://localhost:5013/api/accounting/trial-balance', { headers }),
        axios.get('http://localhost:5013/api/accounting/journal-entries', { headers })
      ]);

      setBalances(balRes.data);
      setEntries(entryRes.data);
    } catch (error) {
      console.error('Failed to fetch accounting data', error);
    } finally {
      setLoading(false);
    }
  };

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(amount);
  };

  if (loading) return <div className="flex items-center justify-center h-full text-indigo-600">Loading Financial Data...</div>;

  const totalAssets = balances.filter(b => b.typeName.includes('Tài sản')).reduce((acc, b) => acc + b.balance, 0);
  const totalRevenue = balances.filter(b => b.typeName.includes('Doanh thu')).reduce((acc, b) => acc + b.balance, 0);
  const totalExpense = balances.filter(b => b.typeName.includes('Chi phí')).reduce((acc, b) => acc + b.balance, 0);

  return (
    <div className="space-y-6">
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-2xl font-bold text-slate-800">Financial Reports</h1>
          <p className="text-slate-500 text-sm">Real-time General Ledger & Trial Balance</p>
        </div>
        <button 
          onClick={fetchData}
          className="px-4 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 transition-colors flex items-center gap-2 text-sm font-medium"
        >
          <Activity size={16} />
          Refresh Data
        </button>
      </div>

      {/* Summary Cards */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        <div className="bg-white p-6 rounded-2xl shadow-sm border border-slate-100 flex items-center gap-4">
          <div className="w-12 h-12 bg-indigo-100 rounded-xl flex items-center justify-center text-indigo-600">
            <Landmark size={24} />
          </div>
          <div>
            <p className="text-sm font-medium text-slate-500 uppercase tracking-wider">Total Assets</p>
            <p className="text-xl font-bold text-slate-800">{formatCurrency(totalAssets)}</p>
          </div>
        </div>
        <div className="bg-white p-6 rounded-2xl shadow-sm border border-slate-100 flex items-center gap-4">
          <div className="w-12 h-12 bg-emerald-100 rounded-xl flex items-center justify-center text-emerald-600">
            <TrendingUp size={24} />
          </div>
          <div>
            <p className="text-sm font-medium text-slate-500 uppercase tracking-wider">Total Revenue</p>
            <p className="text-xl font-bold text-slate-800">{formatCurrency(totalRevenue)}</p>
          </div>
        </div>
        <div className="bg-white p-6 rounded-2xl shadow-sm border border-slate-100 flex items-center gap-4">
          <div className="w-12 h-12 bg-rose-100 rounded-xl flex items-center justify-center text-rose-600">
            <TrendingDown size={24} />
          </div>
          <div>
            <p className="text-sm font-medium text-slate-500 uppercase tracking-wider">Total Expenses</p>
            <p className="text-xl font-bold text-slate-800">{formatCurrency(totalExpense)}</p>
          </div>
        </div>
      </div>

      <div className="grid grid-cols-1 xl:grid-cols-2 gap-6">
        {/* Trial Balance Table */}
        <div className="bg-white rounded-2xl shadow-sm border border-slate-100 overflow-hidden">
          <div className="px-6 py-4 border-b border-slate-100 flex items-center gap-2">
            <PieChart size={18} className="text-indigo-500" />
            <h3 className="font-semibold text-slate-800">Trial Balance (Bảng Cân đối Thử)</h3>
          </div>
          <div className="overflow-x-auto">
            <table className="w-full text-left text-sm">
              <thead className="bg-slate-50 text-slate-600 font-medium">
                <tr>
                  <th className="px-6 py-3">Code</th>
                  <th className="px-6 py-3">Account Name</th>
                  <th className="px-6 py-3 text-right">Balance</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {balances.map((acc) => (
                  <tr key={acc.id} className="hover:bg-slate-50 transition-colors">
                    <td className="px-6 py-3 font-mono font-bold text-indigo-600">{acc.code}</td>
                    <td className="px-6 py-3">
                      <p className="font-medium text-slate-800">{acc.name}</p>
                      <p className="text-[10px] text-slate-400 uppercase">{acc.typeName}</p>
                    </td>
                    <td className="px-6 py-3 text-right font-semibold text-slate-700">
                      {formatCurrency(acc.balance)}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>

        {/* Recent Journal Entries */}
        <div className="bg-white rounded-2xl shadow-sm border border-slate-100 overflow-hidden">
          <div className="px-6 py-4 border-b border-slate-100 flex items-center gap-2">
            <ReceiptText size={18} className="text-indigo-500" />
            <h3 className="font-semibold text-slate-800">Recent Journal Entries (Sổ Nhật Ký)</h3>
          </div>
          <div className="p-6 space-y-4 max-h-[500px] overflow-y-auto">
            {entries.map((entry) => (
              <div key={entry.id} className="bg-slate-50 rounded-xl p-4 border border-slate-100">
                <div className="flex justify-between items-start mb-3">
                  <div>
                    <p className="text-xs font-bold text-indigo-500 uppercase">{entry.reference}</p>
                    <p className="text-sm font-medium text-slate-700">{entry.description}</p>
                  </div>
                  <p className="text-[10px] text-slate-400">{new Date(entry.entryDate).toLocaleDateString()}</p>
                </div>
                <div className="space-y-1">
                  {entry.lines.map((line, idx) => (
                    <div key={idx} className="flex justify-between text-xs">
                      <span className={`${line.debit > 0 ? 'pl-0' : 'pl-6'} text-slate-600`}>
                        {line.code} - {line.name}
                      </span>
                      <div className="flex gap-4">
                        {line.debit > 0 && <span className="font-bold text-emerald-600 w-24 text-right">Dr {formatCurrency(line.debit)}</span>}
                        {line.credit > 0 && <span className="font-bold text-rose-600 w-24 text-right">Cr {formatCurrency(line.credit)}</span>}
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
};
