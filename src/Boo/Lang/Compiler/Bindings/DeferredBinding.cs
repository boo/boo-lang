namespace Boo.Lang.Compiler.Bindings
{
	using System;
	using Boo.Lang.Ast;	 	
	
	public class DeferredBindingResolvedArgs : EventArgs
	{
		public IBinding Binding;
		public Node Node;
		
		public DeferredBindingResolvedArgs(IBinding binding, Node node)
		{
			Binding = binding;
			Node = node;
		}
	}
	
	public delegate void DeferredBindingResolvedHandler(object sender, DeferredBindingResolvedArgs args);

	/// <summary>
	/// A binding for nodes that depend on other nodes or
	/// external conditions before they can be bound/resolved.
	/// </summary>
	public class DeferredBinding : AbstractInternalBinding, IBinding
	{
		protected Node _node;
		
		protected DeferredBindingResolvedHandler _handler;
		
		public DeferredBinding(Node node, DeferredBindingResolvedHandler handler)
		{			
			if (null == node)
			{
				throw new ArgumentNullException("node");
			}
			
			if (null == handler)
			{
				throw new ArgumentNullException("handler");
			}			
			
			_node = node;
			_handler = handler;
		}
		
		public virtual BindingType BindingType
		{
			get
			{
				return BindingType.Deferred;
			}
		}
		
		public string Name
		{
			get
			{
				return "Deferred";
			}
		}		
		
		public void OnDependencyResolved(object sender, EventArgs args)
		{
			_handler(this, new DeferredBindingResolvedArgs((IBinding)sender, _node));
			base.OnResolved();
		}
	}
}
