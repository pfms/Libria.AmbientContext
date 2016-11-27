#if NET46
using System.Threading;
#endif
using System;
using System.Runtime.CompilerServices;
using Libria.AmbientContext.Interfaces;
#if NET45
using System.Runtime.Remoting.Messaging;

#endif

namespace Libria.AmbientContext
{
	public abstract class BaseScope<TImpl, TData> : IScope where TImpl : BaseScope<TImpl, TData>
	{
		private readonly string _instanceIdentifier = Guid.NewGuid().ToString("N");
		private readonly ScopeOption _scopeOption;
		protected readonly bool Nested;
		private TImpl _savedScope;
		protected bool Disposed;
		protected TData ScopeData;

		protected BaseScope(TData fallbackScopeData = default(TData), ScopeOption option = ScopeOption.Required)
		{
			_scopeOption = option;
			Disposed = false;

			if (option == ScopeOption.Suppress)
			{
				_savedScope = AmbientScope.GetCurrentScope();
				AmbientScope.HideScope();
				return;
			}

			ParentScope = AmbientScope.GetCurrentScope();

			if (ParentScope != null && option == ScopeOption.Required)
			{
				Nested = true;
				ScopeData = ParentScope.ScopeData;
			}
			else
			{
				Nested = false;
				ScopeData = fallbackScopeData;
			}

			AmbientScope.SetCurrentScope((TImpl) this);
		}

		protected TImpl ParentScope { get; }
		public static TImpl Current => AmbientScope.GetCurrentScope();

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		~BaseScope()
		{
			Dispose(false);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (Disposed)
			{
				return;
			}

			if (!disposing)
			{
				return;
			}

			if (_scopeOption == ScopeOption.Suppress)
			{
				AmbientScope.SetCurrentScope(_savedScope);
				_savedScope = null;
				Disposed = true;
				return;
			}

			var scope = AmbientScope.GetCurrentScope();
			if (scope != this)
			{
				throw new InvalidOperationException("Scopes must be disposed of in order in which they were created");
			}

			AmbientScope.RemoveScope();

			if (ParentScope != null)
			{
				if (ParentScope.Disposed)
				{
					throw new Exception("Something went terribly wrong with the order of disposing");
				}

				if (_scopeOption == ScopeOption.Required)
				{
					PassScopeDataToParent();
				}

				AmbientScope.SetCurrentScope(ParentScope);
			}
			Disposed = true;
		}

		protected virtual void PassScopeDataToParent()
		{
			ParentScope.ScopeData = ScopeData;
		}

		internal static class AmbientScope
		{
			private static readonly AsyncLocal<string> CurrentInstance = new AsyncLocal<string>();

			private static readonly ConditionalWeakTable<string, TImpl> ScopeInstances =
				new ConditionalWeakTable<string, TImpl>();

			internal static void RemoveScope()
			{
				var current = CurrentInstance.Value;
				CurrentInstance.Value = null;

				if (current != null)
				{
					ScopeInstances.Remove(current);
				}
			}

			internal static void SetCurrentScope(TImpl value)
			{
				var current = CurrentInstance.Value;

				if (current == value._instanceIdentifier)
					return;

				CurrentInstance.Value = value._instanceIdentifier;
				ScopeInstances.GetValue(value._instanceIdentifier, key => value);
			}

			internal static TImpl GetCurrentScope()
			{
				var current = CurrentInstance.Value;
				if (current == null)
					return null;

				TImpl baseScope;

				if (ScopeInstances.TryGetValue(current, out baseScope))
					return baseScope;

				throw new Exception("Scope wasn't disposed properly");
			}

			internal static void HideScope()
			{
				CurrentInstance.Value = null;
			}
		}

#if NET45
		private class AsyncLocal<TVal> where TVal : class
		{
			private static readonly string AsyncLocalIdentifier = "AsyncLocal_" + Guid.NewGuid().ToString("N");

			public TVal Value
			{
				get { return GetValue(); }
				set { SetValue(value); }
			}

			private void SetValue(TVal value)
			{
				CallContext.LogicalSetData(AsyncLocalIdentifier, value);
			}

			private TVal GetValue()
			{
				return CallContext.LogicalGetData(AsyncLocalIdentifier) as TVal;
			}
		}
#endif
	}
}