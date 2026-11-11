import { Component, ErrorInfo, ReactNode } from 'react';
import { AlertTriangle, RefreshCw } from 'lucide-react';
import i18n from '@/i18n/i18n';

interface Props {
  children: ReactNode;
}

interface State {
  hasError: boolean;
  error?: Error;
}

export class ErrorBoundary extends Component<Props, State> {
  public state: State = {
    hasError: false,
  };

  public static getDerivedStateFromError(error: Error): State {
    return { hasError: true, error };
  }

  public componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    console.error('ErrorBoundary caught an unhandled error:', error, errorInfo);
  }

  private handleReload = () => {
    window.location.reload();
  };

  public render() {
    if (this.state.hasError) {
      return (
        <div className="min-h-screen bg-slate-50 flex items-center justify-center p-4">
          <div className="max-w-md w-full bg-white rounded-2xl shadow-lg border border-red-100 p-6 text-center">
            <div className="w-14 h-14 bg-red-50 text-red-600 rounded-full flex items-center justify-center mx-auto mb-4">
              <AlertTriangle className="w-8 h-8" />
            </div>
            <h1 className="text-xl font-bold text-slate-900 mb-2">
              {i18n.t('common.errorBoundaryTitle')}
            </h1>
            <p className="text-slate-600 text-sm mb-6">{i18n.t('common.errorBoundaryDesc')}</p>
            {this.state.error?.message && (
              <div className="bg-slate-100 rounded-lg p-3 text-xs text-slate-700 font-mono text-left mb-6 overflow-x-auto">
                {this.state.error.message}
              </div>
            )}
            <button
              type="button"
              onClick={this.handleReload}
              className="w-full inline-flex items-center justify-center gap-2 bg-emerald-600 hover:bg-emerald-700 text-white font-medium py-2.5 px-4 rounded-xl transition shadow-sm"
            >
              <RefreshCw className="w-4 h-4" />
              {i18n.t('common.reload')}
            </button>
          </div>
        </div>
      );
    }

    return this.props.children;
  }
}
