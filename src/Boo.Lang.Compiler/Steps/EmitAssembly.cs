#region license
// Copyright (c) 2004, Rodrigo B. de Oliveira (rbo@acm.org)
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without modification,
// are permitted provided that the following conditions are met:
//
//     * Redistributions of source code must retain the above copyright notice,
//     this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright notice,
//     this list of conditions and the following disclaimer in the documentation
//     and/or other materials provided with the distribution.
//     * Neither the name of Rodrigo B. de Oliveira nor the names of its
//     contributors may be used to endorse or promote products derived from this
//     software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
// ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE
// FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
// SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
// CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
// OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF
// THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
#endregion

using System;
using System.Collections;
using System.Diagnostics;
using System.Diagnostics.SymbolStore;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Resources;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Boo.Lang.Compiler.Ast;
using Boo.Lang.Compiler.TypeSystem;
using Boo.Lang.Runtime;
using Attribute = Boo.Lang.Compiler.Ast.Attribute;
using Module = Boo.Lang.Compiler.Ast.Module;

namespace Boo.Lang.Compiler.Steps
{
	class LoopInfo
	{
		public Label BreakLabel;
		
		public Label ContinueLabel;
		
		public int TryBlockDepth;
		
		public LoopInfo(Label breakLabel, Label continueLabel, int tryBlockDepth)
		{
			BreakLabel = breakLabel;
			ContinueLabel = continueLabel;
			TryBlockDepth = tryBlockDepth;
		}
	}
	
	public class EmitAssembly : AbstractVisitorCompilerStep
	{
		static ConstructorInfo DebuggableAttribute_Constructor = typeof(DebuggableAttribute).GetConstructor(new Type[] { Types.Bool, Types.Bool });

		static ConstructorInfo DuckTypedAttribute_Constructor = Types.DuckTypedAttribute.GetConstructor(new Type[0]);
		
		static ConstructorInfo ParamArrayAttribute_Constructor = Types.ParamArrayAttribute.GetConstructor(new Type[0]);
		
		static MethodInfo RuntimeServices_NormalizeArrayIndex = Types.RuntimeServices.GetMethod("NormalizeArrayIndex");
		
		static MethodInfo RuntimeServices_ToBool_Object = Types.RuntimeServices.GetMethod("ToBool", new Type[] { Types.Object });

		static MethodInfo RuntimeServices_ToBool_Decimal = Types.RuntimeServices.GetMethod("ToBool", new Type[] { Types.Decimal });

		static MethodInfo Builtins_ArrayTypedConstructor = Types.Builtins.GetMethod("array", new Type[] { Types.Type, Types.Int });
		
		static MethodInfo Builtins_ArrayTypedCollectionConstructor = Types.Builtins.GetMethod("array", new Type[] { Types.Type, Types.ICollection });
		
		static MethodInfo Math_Pow = typeof(Math).GetMethod("Pow");
		
		static ConstructorInfo List_EmptyConstructor = Types.List.GetConstructor(Type.EmptyTypes);
		
		static ConstructorInfo List_ArrayBoolConstructor = Types.List.GetConstructor(new Type[] { Types.ObjectArray, Types.Bool });
		
		static ConstructorInfo Hash_Constructor = Types.Hash.GetConstructor(new Type[0]);
		
		static ConstructorInfo Regex_Constructor = typeof(Regex).GetConstructor(new Type[] { Types.String });
		
		static MethodInfo Hash_Add = Types.Hash.GetMethod("Add", new Type[] { typeof(object), typeof(object) });
		
		static ConstructorInfo TimeSpan_LongConstructor = Types.TimeSpan.GetConstructor(new Type[] { typeof(long) });
		
		static MethodInfo Type_GetTypeFromHandle = Types.Type.GetMethod("GetTypeFromHandle");
		
		AssemblyBuilder _asmBuilder;
		
		ModuleBuilder _moduleBuilder;
		
		Hashtable _symbolDocWriters = new Hashtable();
		
		// IL generation state
		ILGenerator _il;
		Label _returnLabel; // current label for method return
		LocalBuilder _returnValueLocal; // returnValueLocal
		IType _returnType;
		int _tryBlock; // are we in a try block?
		bool _checked = true;
		bool _rawArrayIndexing = false;
		Hashtable _typeCache = new Hashtable();
		
		// keeps track of types on the IL stack
		Stack _types = new Stack();
		
		Stack _loopInfoStack = new Stack();
		
		AttributeCollection _assemblyAttributes = new AttributeCollection();
		
		LoopInfo _currentLoopInfo;
		
		void EnterLoop(Label breakLabel, Label continueLabel)
		{
			_loopInfoStack.Push(_currentLoopInfo);
			_currentLoopInfo = new LoopInfo(breakLabel, continueLabel, _tryBlock);
		}
		
		bool InTryInLoop()
		{
			return _tryBlock > _currentLoopInfo.TryBlockDepth;
		}
		
		void LeaveLoop()
		{
			_currentLoopInfo = (LoopInfo)_loopInfoStack.Pop();
		}
		
		void PushType(IType type)
		{
			_types.Push(type);
		}
		
		void PushBool()
		{
			PushType(TypeSystemServices.BoolType);
		}
		
		void PushVoid()
		{
			PushType(TypeSystemServices.VoidType);
		}
		
		IType PopType()
		{
			return (IType)_types.Pop();
		}
		
		IType PeekTypeOnStack()
		{
			return (IType)_types.Peek();
		}
		
		void AssertStackIsEmpty(string message)
		{
			if (0 != _types.Count)
			{
				throw new ApplicationException(
					string.Format("{0}: {1} items still on the stack.", message, _types.Count)
				);
			}
		}
		
		override public void Run()
		{
			if (Errors.Count > 0)
			{
				return;
			}
			
			GatherAssemblyAttributes();
			SetUpAssembly();
			
			DefineTypes();
			
			DefineResources();
			DefineAssemblyAttributes();
			DefineEntryPoint();
		}
		
		void GatherAssemblyAttributes()
		{
			foreach (Module module in CompileUnit.Modules)
			{
				foreach (Attribute attribute in module.AssemblyAttributes)
				{
					_assemblyAttributes.Add(attribute);
				}
			}
		}
		
		void DefineTypes()
		{
			if (CompileUnit.Modules.Count > 0)
			{
				List types = CollectTypes();
				
				foreach (TypeDefinition type in types)
				{
					DefineType(type);
				}
				
				foreach (TypeDefinition type in types)
				{
					DefineTypeMembers(type);
				}
				
				foreach (Module module in CompileUnit.Modules)
				{
					OnModule(module);
				}
				
				EmitAttributes();
				CreateTypes(types);
			}
		}
		
		class AttributeEmitVisitor : DepthFirstVisitor
		{
			EmitAssembly _emitter;
			
			public AttributeEmitVisitor(EmitAssembly emitter)
			{
				_emitter = emitter;
			}
			
			public override void OnField(Field node)
			{
				_emitter.EmitFieldAttributes(node);
			}
			
			public override void OnEnumMember(EnumMember node)
			{
				_emitter.EmitFieldAttributes(node);
			}
			
			public override void OnEvent(Event node)
			{
				_emitter.EmitEventAttributes(node);
			}
			
			public override void OnProperty(Property node)
			{
				Visit(node.Getter);
				Visit(node.Setter);
				_emitter.EmitPropertyAttributes(node);
			}
			
			public override void OnConstructor(Constructor node)
			{
				Visit(node.Parameters);
				_emitter.EmitConstructorAttributes(node);
			}
			
			public override void OnMethod(Method node)
			{
				Visit(node.Parameters);
				_emitter.EmitMethodAttributes(node);
			}
			
			public override void OnParameterDeclaration(ParameterDeclaration node)
			{
				_emitter.EmitParameterAttributes(node);
			}
			
			public override void LeaveClassDefinition(ClassDefinition node)
			{
				_emitter.EmitTypeAttributes(node);
			}
			
			public override void LeaveInterfaceDefinition(InterfaceDefinition node)
			{
				_emitter.EmitTypeAttributes(node);
			}
			
			public override void LeaveEnumDefinition(EnumDefinition node)
			{
				_emitter.EmitTypeAttributes(node);
			}
		}
		
		delegate void CustomAttributeSetter(CustomAttributeBuilder attribute);
		
		void EmitAttributes(INodeWithAttributes node, CustomAttributeSetter setCustomAttribute)
		{
			foreach (Attribute attribute in node.Attributes)
			{
				setCustomAttribute(GetCustomAttributeBuilder(attribute));
			}
		}
		
		void EmitPropertyAttributes(Property node)
		{
			PropertyBuilder builder = GetPropertyBuilder(node);
			EmitAttributes(node, new CustomAttributeSetter(builder.SetCustomAttribute));
		}
		
		void EmitParameterAttributes(ParameterDeclaration node)
		{
			ParameterBuilder builder = (ParameterBuilder)GetBuilder(node);
			EmitAttributes(node, new CustomAttributeSetter(builder.SetCustomAttribute));
		}
		
		void EmitEventAttributes(Event node)
		{
			EventBuilder builder = (EventBuilder)GetBuilder(node);
			EmitAttributes(node, new CustomAttributeSetter(builder.SetCustomAttribute));
		}
		
		void EmitConstructorAttributes(Constructor node)
		{
			ConstructorBuilder builder = (ConstructorBuilder)GetBuilder(node);
			EmitAttributes(node, new CustomAttributeSetter(builder.SetCustomAttribute));
		}
		
		void EmitMethodAttributes(Method node)
		{
			MethodBuilder builder = GetMethodBuilder(node);
			EmitAttributes(node, new CustomAttributeSetter(builder.SetCustomAttribute));
		}
		
		void EmitTypeAttributes(TypeDefinition node)
		{
			TypeBuilder builder = GetTypeBuilder(node);
			EmitAttributes(node, new CustomAttributeSetter(builder.SetCustomAttribute));
		}
		
		void EmitFieldAttributes(TypeMember node)
		{
			FieldBuilder builder = GetFieldBuilder(node);
			EmitAttributes(node, new CustomAttributeSetter(builder.SetCustomAttribute));
		}
		
		void EmitAttributes()
		{
			AttributeEmitVisitor visitor = new AttributeEmitVisitor(this);
			foreach (Module module in CompileUnit.Modules)
			{
				module.Accept(visitor);
			}
		}
		
		void CreateTypes(List types)
		{
			new TypeCreator(this, types).Run();
		}
		
		/// <summary>
		/// Ensures that all types are created in the correct order.
		/// </summary>
		class TypeCreator
		{
			EmitAssembly _emitter;
			
			Hashtable _created;
			
			List _types;
			
			TypeMember _current;
			
			public TypeCreator(EmitAssembly emitter, List types)
			{
				_emitter = emitter;
				_types = types;
				_created = new Hashtable();
			}
			
			public void Run()
			{
				ResolveEventHandler resolveHandler = new ResolveEventHandler(OnTypeResolve);
				AppDomain current = Thread.GetDomain();
				
				try
				{
					current.TypeResolve += resolveHandler;
					CreateTypes();
				}
				finally
				{
					current.TypeResolve -= resolveHandler;
				}
			}
			
			void CreateTypes()
			{
				foreach (TypeMember type in _types)
				{
					CreateType(type);
				}
			}
			
			void CreateType(TypeMember type)
			{
				if (!_created.ContainsKey(type))
				{
					TypeMember saved = _current;
					_current = type;
					
					_created.Add(type, type);
					
					Trace("creating type '{0}'", type);
					
					if (IsNestedType(type))
					{
						CreateType((TypeMember)type.ParentNode);
					}
					
					TypeDefinition typedef = type as TypeDefinition;
					if (null != typedef)
					{
						foreach (TypeReference baseTypeRef in typedef.BaseTypes)
						{
							IType baseType = _emitter.GetType(baseTypeRef);
							
							AbstractInternalType tag = baseType as AbstractInternalType;
							if (null != tag)
							{
								CreateType(tag.TypeDefinition);
							}

#if NET_2_0
							// If base type is generic, create any internal parameters it might have
							if (baseType.GenericTypeInfo != null)
							{
								foreach (IType argument in baseType.GenericTypeInfo.GenericArguments)
								{
									tag = argument as AbstractInternalType;
									if (null != tag)
									{
										CreateType(tag.TypeDefinition);
									}
								}
							}
#endif
						}
					}
					
					_emitter.GetTypeBuilder(type).CreateType();
					
					Trace("type '{0}' successfully created", type);
					
					_current = saved;
				}
			}
			
			bool IsNestedType(TypeMember type)
			{
				NodeType parent = type.ParentNode.NodeType;
				return (NodeType.ClassDefinition == parent) ||
					(NodeType.InterfaceDefinition == parent);
			}
			
			Assembly OnTypeResolve(object sender, ResolveEventArgs args)
			{
				Trace("OnTypeResolve('{0}') during '{1}' creation.", args.Name, _current);
				
				// TypeResolve is generated whenever a type
				// contains fields of a value type not created yet.
				// All we need to do is look for value type fields
				// and create them all.
				ClassDefinition classdef = _current as ClassDefinition;
				foreach (TypeMember member in classdef.Members)
				{
					if (NodeType.Field == member.NodeType)
					{
						AbstractInternalType type = _emitter.GetType(((Field)member).Type) as AbstractInternalType;
						if (type != null && type.IsValueType)
						{
							CreateType(type.TypeDefinition);
						}
					}
				}

				return _emitter._asmBuilder;
			}
			
			void Trace(string format, params object[] args)
			{
				_emitter.Context.TraceVerbose(format, args);
			}
		}
		
		List CollectTypes()
		{
			List types = new List();
			foreach (Module module in CompileUnit.Modules)
			{
				CollectTypes(types, module.Members);
			}
			return types;
		}
		
		void CollectTypes(List types, TypeMemberCollection members)
		{
			foreach (TypeMember member in members)
			{
				switch (member.NodeType)
				{
					case NodeType.InterfaceDefinition:
					case NodeType.ClassDefinition:
						{
							types.Add(member);
							CollectTypes(types, ((TypeDefinition)member).Members);
							break;
						}
					case NodeType.EnumDefinition:
						{
							types.Add(member);
							break;
						}
				}
			}
		}
		
		override public void Dispose()
		{
			base.Dispose();
			
			_asmBuilder = null;
			_moduleBuilder = null;
			_symbolDocWriters.Clear();
			_il = null;
			_returnValueLocal = null;
			_returnType = null;
			_tryBlock = 0;
			_checked = true;
			_rawArrayIndexing = false;
			_types.Clear();
			_typeCache.Clear();
			_builders.Clear();
			_assemblyAttributes.Clear();
		}
		
		override public void OnAttribute(Attribute node)
		{
		}
		
		override public void OnModule(Module module)
		{
			Visit(module.Members);
		}

		override public void OnEnumDefinition(EnumDefinition node)
		{
			Type baseType = typeof(int);
			
			TypeBuilder builder = GetTypeBuilder(node);
			
			builder.DefineField("value__", baseType,
			                    FieldAttributes.Public |
			                    FieldAttributes.SpecialName |
			                    FieldAttributes.RTSpecialName);
			
			foreach (EnumMember member in node.Members)
			{
				FieldBuilder field = builder.DefineField(member.Name, builder,
				                                         FieldAttributes.Public |
				                                         FieldAttributes.Static |
				                                         FieldAttributes.Literal);
				field.SetConstant((int)member.Initializer.Value);
				SetBuilder(member, field);
			}
		}
		
		override public void OnArrayTypeReference(ArrayTypeReference node)
		{
		}
		
		override public void OnClassDefinition(ClassDefinition node)
		{
			EmitTypeDefinition(node);
		}
		
		override public void OnField(Field node)
		{
			FieldBuilder builder = GetFieldBuilder(node);
			if (builder.IsLiteral)
			{
				builder.SetConstant(GetInternalFieldStaticValue((InternalField)node.Entity));
			}
		}
		
		override public void OnInterfaceDefinition(InterfaceDefinition node)
		{
			TypeBuilder builder = GetTypeBuilder(node);
			foreach (TypeReference baseType in node.BaseTypes)
			{
				builder.AddInterfaceImplementation(GetSystemType(baseType));
			}
		}
		
		override public void OnCallableDefinition(CallableDefinition node)
		{
			NotImplemented(node, "Unexpected callable definition!");
		}
		
		void EmitTypeDefinition(TypeDefinition node)
		{
			TypeBuilder current = GetTypeBuilder(node);
			EmitBaseTypesAndAttributes(node, current);
			Visit(node.Members);
		}
		
		override public void OnMethod(Method method)
		{
			if (method.IsRuntime) return;
			if (IsPInvoke(method)) return;
			
			MethodBuilder methodBuilder = GetMethodBuilder(method);
			if (null != method.ExplicitInfo)
			{
				IMethod ifaceMethod = (IMethod)method.ExplicitInfo.Entity;
				MethodInfo ifaceInfo = GetMethodInfo(ifaceMethod);
				MethodInfo implInfo = GetMethodInfo((IMethod)method.Entity);

				TypeBuilder typeBuilder = GetTypeBuilder(method.DeclaringType);
				typeBuilder.DefineMethodOverride(implInfo, ifaceInfo);
			}

			EmitMethod(method, methodBuilder.GetILGenerator());
		}
		
		void EmitMethod(Method method, ILGenerator generator)
		{
			_il = generator;
			
			DefineLabels(method);
			Visit(method.Locals);
			
			BeginMethodBody(GetEntity(method).ReturnType);
			Visit(method.Body);
			EndMethodBody();
		}
		
		void BeginMethodBody(IType returnType)
		{
			_returnType = returnType;
			_returnLabel = _il.DefineLabel();
			if (TypeSystemServices.VoidType != _returnType)
			{
				_returnValueLocal = _il.DeclareLocal(GetSystemType(_returnType));
			}
		}
		
		void EndMethodBody()
		{
			_il.MarkLabel(_returnLabel);
			if (null != _returnValueLocal)
			{
				_il.Emit(OpCodes.Ldloc, _returnValueLocal);
				_returnValueLocal = null;
			}
			_il.Emit(OpCodes.Ret);
		}

		private bool IsPInvoke(Method method)
		{
			return GetEntity(method).IsPInvoke;
		}

		override public void OnBlock(Block block)
		{
			bool currentChecked = _checked;
			_checked = AstAnnotations.IsChecked(block, Parameters.Checked);
			
			bool currentArrayIndexing = _rawArrayIndexing;
			_rawArrayIndexing = AstAnnotations.IsRawIndexing(block);

			Visit(block.Statements);

			_rawArrayIndexing = currentArrayIndexing;
			_checked = currentChecked;
		}

		void DefineLabels(Method method)
		{
			foreach (InternalLabel label in ((InternalMethod)method.Entity).Labels)
			{
				label.Label = _il.DefineLabel();
			}
		}
		
		override public void OnConstructor(Constructor constructor)
		{
			if (constructor.IsRuntime) return;
			
			ConstructorBuilder builder = GetConstructorBuilder(constructor);
			EmitMethod(constructor, builder.GetILGenerator());
		}
		
		override public void OnLocal(Local local)
		{
			InternalLocal info = GetInternalLocal(local);
			info.LocalBuilder = _il.DeclareLocal(GetSystemType(local));
			if (Parameters.Debug)
			{
				info.LocalBuilder.SetLocalSymInfo(local.Name);
			}
		}
		
		override public void OnForStatement(ForStatement node)
		{
			NotImplemented("ForStatement");
		}
		
		override public void OnReturnStatement(ReturnStatement node)
		{
			EmitDebugInfo(node);
			OpCode retOpCode = _tryBlock > 0 ? OpCodes.Leave : OpCodes.Br;
			
			if (null != node.Expression)
			{
				Visit(node.Expression);
				EmitCastIfNeeded(_returnType, PopType());
				_il.Emit(OpCodes.Stloc, _returnValueLocal);
			}
			_il.Emit(retOpCode, _returnLabel);
		}
		
		override public void OnRaiseStatement(RaiseStatement node)
		{
			EmitDebugInfo(node);
			if (node.Exception == null)
			{
				_il.Emit(OpCodes.Rethrow);
			}
			else
			{
				Visit(node.Exception); PopType();
				_il.Emit(OpCodes.Throw);
			}
		}

		override public void OnTryStatement(TryStatement node)
		{
			++_tryBlock;
			_il.BeginExceptionBlock();
			Visit(node.ProtectedBlock);
			Visit(node.ExceptionHandlers);
			--_tryBlock;
			
			if (null != node.EnsureBlock)
			{
				_il.BeginFinallyBlock();
				Visit(node.EnsureBlock);
			}
			_il.EndExceptionBlock();
			
			
		}
		
		override public void OnExceptionHandler(ExceptionHandler node)
		{
			_il.BeginCatchBlock(GetSystemType(node.Declaration));
			_il.Emit(OpCodes.Stloc, GetLocalBuilder(node.Declaration));
			Visit(node.Block);
		}
		
		override public void OnUnpackStatement(UnpackStatement node)
		{
			NotImplemented("Unpacking");
		}
		
		override public bool EnterExpressionStatement(ExpressionStatement node)
		{
			EmitDebugInfo(node);
			return true;
		}
		
		override public void LeaveExpressionStatement(ExpressionStatement node)
		{
			// if the type of the inner expression is not
			// void we need to pop its return value to leave
			// the stack sane
			DiscardValueOnStack();
			AssertStackIsEmpty("stack must be empty after a statement!");
		}
		
		void DiscardValueOnStack()
		{
			if (PopType() != TypeSystemServices.VoidType)
			{
				_il.Emit(OpCodes.Pop);
			}
		}
		
		override public void OnUnlessStatement(UnlessStatement node)
		{
			Label endLabel = _il.DefineLabel();
			EmitDebugInfo(node);
			EmitBranchTrue(node.Condition, endLabel);
			node.Block.Accept(this);
			_il.MarkLabel(endLabel);
		}
		
		void OnSwitch(MethodInvocationExpression node)
		{
			ExpressionCollection args = node.Arguments;
			Visit(args[0]);
			EmitCastIfNeeded(TypeSystemServices.IntType, PopType());
			
			Label[] labels = new Label[args.Count-1];
			for (int i=0; i<labels.Length; ++i)
			{
				labels[i] = ((InternalLabel)args[i+1].Entity).Label;
			}
			_il.Emit(OpCodes.Switch, labels);
			
			PushVoid();
		}
		
		override public void OnGotoStatement(GotoStatement node)
		{
			EmitDebugInfo(node);
			
			InternalLabel label = (InternalLabel)GetEntity(node.Label);
			int gotoDepth = AstAnnotations.GetTryBlockDepth(node);
			int targetDepth = AstAnnotations.GetTryBlockDepth(label.LabelStatement);
			
			if (targetDepth == gotoDepth)
			{
				_il.Emit(OpCodes.Br, label.Label);
			}
			else
			{
				_il.Emit(OpCodes.Leave, label.Label);
			}
		}
		
		override public void OnLabelStatement(LabelStatement node)
		{
			EmitDebugInfo(node);
			_il.MarkLabel(((InternalLabel)node.Entity).Label);
		}
		
		override public void OnConditionalExpression(ConditionalExpression node)
		{
			IType type = GetExpressionType(node);
			
			Label endLabel = _il.DefineLabel();
			
			EmitBranchFalse(node.Condition, endLabel);
			node.TrueValue.Accept(this);
			EmitCastIfNeeded(type, PopType());
			
			Label elseEndLabel = _il.DefineLabel();
			_il.Emit(OpCodes.Br, elseEndLabel);
			_il.MarkLabel(endLabel);
			
			endLabel = elseEndLabel;
			node.FalseValue.Accept(this);
			EmitCastIfNeeded(type, PopType());
			
			_il.MarkLabel(endLabel);
			
			PushType(type);
		}
		
		override public void OnIfStatement(IfStatement node)
		{
			Label endLabel = _il.DefineLabel();
			
			EmitDebugInfo(node);
			EmitBranchFalse(node.Condition, endLabel);
			
			node.TrueBlock.Accept(this);
			if (null != node.FalseBlock)
			{
				Label elseEndLabel = _il.DefineLabel();
				_il.Emit(OpCodes.Br, elseEndLabel);
				_il.MarkLabel(endLabel);
				
				endLabel = elseEndLabel;
				node.FalseBlock.Accept(this);
			}
			
			_il.MarkLabel(endLabel);
		}
		
		void EmitBranchTrue(UnaryExpression expression, Label label)
		{
			if (UnaryOperatorType.LogicalNot == expression.Operator)
			{
				EmitBranchFalse(expression.Operand, label);
			}
			else
			{
				DefaultBranchTrue(expression, label);
			}
		}
		
		void EmitBranchTrue(BinaryExpression expression, Label label)
		{
			switch (expression.Operator)
			{
				case BinaryOperatorType.TypeTest:
					{
						EmitTypeTest(expression);
						_il.Emit(OpCodes.Brtrue, label);
						break;
					}
					
				case BinaryOperatorType.Or:
					{
						EmitBranchTrue(expression.Left, label);
						EmitBranchTrue(expression.Right, label);
						break;
					}
					
				case BinaryOperatorType.And:
					{
						Label skipRhs = _il.DefineLabel();
						EmitBranchFalse(expression.Left, skipRhs);
						EmitBranchTrue(expression.Right, label);
						_il.MarkLabel(skipRhs);
						break;
					}
					
				case BinaryOperatorType.Equality:
					{
						LoadCmpOperands(expression);
						_il.Emit(OpCodes.Beq, label);
						break;
					}
					
				case BinaryOperatorType.ReferenceEquality:
					{
						Visit(expression.Left); PopType();
						Visit(expression.Right); PopType();
						_il.Emit(OpCodes.Beq, label);
						break;
					}
					
				case BinaryOperatorType.ReferenceInequality:
					{
						if (IsNull(expression.Left))
						{
							EmitRawBranchTrue(expression.Right, label);
							break;
						}
						if (IsNull(expression.Right))
						{
							EmitRawBranchTrue(expression.Left, label);
							break;
						}
						Visit(expression.Left); PopType();
						Visit(expression.Right); PopType();
						_il.Emit(OpCodes.Ceq);
						_il.Emit(OpCodes.Brfalse, label);
						break;
					}
					
				case BinaryOperatorType.GreaterThan:
					{
						LoadCmpOperands(expression);
						_il.Emit(OpCodes.Bgt, label);
						break;
					}
					
				case BinaryOperatorType.GreaterThanOrEqual:
					{
						LoadCmpOperands(expression);
						_il.Emit(OpCodes.Bge, label);
						break;
					}
					
				case BinaryOperatorType.LessThan:
					{
						LoadCmpOperands(expression);
						_il.Emit(OpCodes.Blt, label);
						break;
					}
					
				case BinaryOperatorType.LessThanOrEqual:
					{
						LoadCmpOperands(expression);
						_il.Emit(OpCodes.Ble, label);
						break;
					}
					
				default:
					{
						DefaultBranchTrue(expression, label);
						break;
					}
			}
		}
		
		void EmitRawBranchTrue(Expression expression, Label label)
		{
			expression.Accept(this); PopType();
			_il.Emit(OpCodes.Brtrue, label);
		}
		
		void EmitBranchTrue(Expression expression, Label label)
		{
			switch (expression.NodeType)
			{
				case NodeType.BinaryExpression:
					{
						EmitBranchTrue((BinaryExpression)expression, label);
						break;
					}
					
				case NodeType.UnaryExpression:
					{
						EmitBranchTrue((UnaryExpression)expression, label);
						break;
					}
					
				default:
					{
						DefaultBranchTrue(expression, label);
						break;
					}
			}
		}
		
		void DefaultBranchTrue(Expression expression, Label label)
		{
			expression.Accept(this);
			IType type = PopType();
			if (TypeSystemServices.DoubleType == type)
			{
				_il.Emit(OpCodes.Ldc_R8, 0.0);
				_il.Emit(OpCodes.Bne_Un, label);
			}
			else if (TypeSystemServices.SingleType == type)
			{
				_il.Emit(OpCodes.Ldc_R4, 0.0f);
				_il.Emit(OpCodes.Bne_Un, label);
			}
			else
			{
				EmitToBoolIfNeeded(expression);
				_il.Emit(OpCodes.Brtrue, label);
			}
		}
		
		void EmitBranchFalse(BinaryExpression expression, Label label)
		{
			switch (expression.Operator)
			{
				case BinaryOperatorType.TypeTest:
					{
						EmitTypeTest(expression);
						_il.Emit(OpCodes.Brfalse, label);
						break;
					}
					
				case BinaryOperatorType.Or:
					{
						Label end = _il.DefineLabel();
						EmitBranchTrue(expression.Left, end);
						EmitBranchFalse(expression.Right, label);
						_il.MarkLabel(end);
						break;
					}
					
				case BinaryOperatorType.And:
					{
						EmitBranchFalse(expression.Left, label);
						EmitBranchFalse(expression.Right, label);
						break;
					}

				case BinaryOperatorType.Equality:
					{
						if (CanOptimizeAwayZeroOrFalseComparison(expression.Left, expression.Right))
						{
							EmitBranchTrue(expression.Right, label);
						}
						else if (CanOptimizeAwayZeroOrFalseComparison(expression.Right, expression.Left))
						{
							EmitBranchTrue(expression.Left, label);
						}
						else
						{
							DefaultBranchFalse(expression, label);
						}
						break;
					}

				case BinaryOperatorType.Inequality:
					{
						if (CanOptimizeAwayZeroOrFalseComparison(expression.Left, expression.Right))
						{
							EmitBranchFalse(expression.Right, label);
						}
						else if (CanOptimizeAwayZeroOrFalseComparison(expression.Right, expression.Left))
						{
							EmitBranchFalse(expression.Left, label);
						}
						else
						{
							DefaultBranchFalse(expression, label);
						}
						break;
					}
					
				default:
					{
						DefaultBranchFalse(expression, label);
						break;
					}
			}
		}

		private bool IsNull(Expression expression)
		{
			return NodeType.NullLiteralExpression == expression.NodeType;
		}

		private bool CanOptimizeAwayZeroOrFalseComparison(Expression expression, Expression operand)
		{
			return (IsZero(expression) || IsFalse(expression));
		}

		private bool IsFalse(Expression expression)
		{
			return NodeType.BoolLiteralExpression == expression.NodeType
				&& (false == ((BoolLiteralExpression)expression).Value);
		}

		private bool IsZero(Expression expression)
		{
			return NodeType.IntegerLiteralExpression == expression.NodeType
				&& (0 == ((IntegerLiteralExpression)expression).Value);
		}

		void EmitBranchFalse(Expression expression, Label label)
		{
			switch (expression.NodeType)
			{
				case NodeType.UnaryExpression:
					{
						EmitBranchFalse((UnaryExpression)expression, label);
						break;
					}
					
				case NodeType.BinaryExpression:
					{
						EmitBranchFalse((BinaryExpression)expression, label);
						break;
					}
					
				default:
					{
						DefaultBranchFalse(expression, label);
						break;
					}
			}
		}
		
		void EmitBranchFalse(UnaryExpression expression, Label label)
		{
			switch (expression.Operator)
			{
				case UnaryOperatorType.LogicalNot:
					{
						EmitBranchTrue(expression.Operand, label);
						break;
					}
					
				default:
					{
						DefaultBranchFalse(expression, label);
						break;
					}
			}
		}
		
		void DefaultBranchFalse(Expression expression, Label label)
		{
			expression.Accept(this);
			IType type = PopType();
			if (TypeSystemServices.DoubleType == type)
			{
				_il.Emit(OpCodes.Ldc_R8, (double)0.0);
				_il.Emit(OpCodes.Ceq);
				_il.Emit(OpCodes.Brtrue, label);
			}
			else if (TypeSystemServices.SingleType == type)
			{
				_il.Emit(OpCodes.Ldc_R4, (float)0.0);
				_il.Emit(OpCodes.Ceq);
				_il.Emit(OpCodes.Brtrue, label);
			}
			else
			{
				EmitToBoolIfNeeded(expression);
				_il.Emit(OpCodes.Brfalse, label);
			}
		}
		
		override public void OnBreakStatement(BreakStatement node)
		{
			EmitDebugInfo(node);
			if (InTryInLoop())
			{
				_il.Emit(OpCodes.Leave, _currentLoopInfo.BreakLabel);
			}
			else
			{
				_il.Emit(OpCodes.Br, _currentLoopInfo.BreakLabel);
			}
		}
		
		override public void OnContinueStatement(ContinueStatement node)
		{
			EmitDebugInfo(node);
			if (InTryInLoop())
			{
				_il.Emit(OpCodes.Leave, _currentLoopInfo.ContinueLabel);
			}
			else
			{
				_il.Emit(OpCodes.Br, _currentLoopInfo.ContinueLabel);
			}
		}
		
		override public void OnWhileStatement(WhileStatement node)
		{
			Label endLabel = _il.DefineLabel();
			Label bodyLabel = _il.DefineLabel();
			Label conditionLabel = _il.DefineLabel();
			
			_il.Emit(OpCodes.Br, conditionLabel);
			_il.MarkLabel(bodyLabel);
			
			EnterLoop(endLabel, conditionLabel);
			node.Block.Accept(this);
			LeaveLoop();
			
			_il.MarkLabel(conditionLabel);
			EmitDebugInfo(node);
			EmitBranchTrue(node.Condition, bodyLabel);
			_il.MarkLabel(endLabel);
		}
		
		void EmitIntNot()
		{
			_il.Emit(OpCodes.Ldc_I4_0);
			_il.Emit(OpCodes.Ceq);
		}
		
		void EmitGenericNot()
		{
			// bool codification:
			// value_on_stack ? 0 : 1
			Label wasTrue = _il.DefineLabel();
			Label wasFalse = _il.DefineLabel();
			_il.Emit(OpCodes.Brfalse, wasFalse);
			_il.Emit(OpCodes.Ldc_I4_0);
			_il.Emit(OpCodes.Br, wasTrue);
			_il.MarkLabel(wasFalse);
			_il.Emit(OpCodes.Ldc_I4_1);
			_il.MarkLabel(wasTrue);
		}
		
		override public void OnUnaryExpression(UnaryExpression node)
		{
			switch (node.Operator)
			{
				case UnaryOperatorType.LogicalNot:
					{
						EmitLogicalNot(node);
						break;
					}
					
				case UnaryOperatorType.UnaryNegation:
					{
						EmitUnaryNegation(node);
						break;
					}

				case UnaryOperatorType.OnesComplement:
					{
						EmitOnesComplement(node);
						break;
					}
					
				default:
					{
						NotImplemented(node, "unary operator not supported");
						break;
					}
			}
		}

		private void EmitOnesComplement(UnaryExpression node)
		{
			node.Operand.Accept(this);
			_il.Emit(OpCodes.Not);
		}

		private void EmitLogicalNot(UnaryExpression node)
		{
			Expression operand = node.Operand;
			operand.Accept(this);
			IType typeOnStack = PopType();
			if (IsBoolOrInt(typeOnStack) || EmitToBoolIfNeeded(operand))
			{
				EmitIntNot();
			}
			else
			{
				EmitGenericNot();
			}
			PushBool();
		}

		private void EmitUnaryNegation(UnaryExpression node)
		{
			node.Operand.Accept(this);
			IType type = PopType();
			_il.Emit(OpCodes.Ldc_I4, -1);
			EmitCastIfNeeded(type, TypeSystemServices.IntType);
			_il.Emit(OpCodes.Mul);
			PushType(type);
		}

		bool ShouldLeaveValueOnStack(Expression node)
		{
			return node.ParentNode.NodeType != NodeType.ExpressionStatement;
		}
		
		void OnReferenceComparison(BinaryExpression node)
		{
			node.Left.Accept(this); PopType();
			node.Right.Accept(this); PopType();
			_il.Emit(OpCodes.Ceq);
			if (BinaryOperatorType.ReferenceInequality == node.Operator)
			{
				EmitIntNot();
			}
			PushBool();
		}
		
		void OnAssignmentToSlice(BinaryExpression node)
		{
			SlicingExpression slice = (SlicingExpression)node.Left;
			Visit(slice.Target);
			
			IArrayType arrayType = (IArrayType)PopType();
			IType elementType = arrayType.GetElementType();
			OpCode opcode = GetStoreEntityOpCode(elementType);
			
			Slice index = slice.Indices[0];
			EmitNormalizedArrayIndex(slice, index.Begin);
			
			bool stobj = IsStobj(opcode);
			if (stobj)
			{
				_il.Emit(OpCodes.Ldelema, GetSystemType(elementType));
			}
			
			Visit(node.Right);
			EmitCastIfNeeded(elementType, PopType());
			
			bool leaveValueOnStack = ShouldLeaveValueOnStack(node);
			LocalBuilder temp = null;
			if (leaveValueOnStack)
			{
				_il.Emit(OpCodes.Dup);
				temp = StoreTempLocal(elementType);
			}
			
			if (stobj)
			{
				_il.Emit(opcode, GetSystemType(elementType));
			}
			else
			{
				_il.Emit(opcode);
			}
			
			if (leaveValueOnStack)
			{
				LoadLocal(temp, elementType);
			}
			else
			{
				PushVoid();
			}
		}

		private void LoadLocal(LocalBuilder local, IType localType)
		{
			_il.Emit(OpCodes.Ldloc, local);
			PushType(localType);
		}

		private LocalBuilder StoreTempLocal(IType elementType)
		{
			LocalBuilder temp;
			temp = _il.DeclareLocal(GetSystemType(elementType));
			_il.Emit(OpCodes.Stloc, temp);
			return temp;
		}

		void OnAssignment(BinaryExpression node)
		{
			if (NodeType.SlicingExpression == node.Left.NodeType)
			{
				OnAssignmentToSlice(node);
				return;
			}
			
			// when the parent is not a statement we need to leave
			// the value on the stack
			bool leaveValueOnStack = ShouldLeaveValueOnStack(node);
			IEntity tag = TypeSystemServices.GetEntity(node.Left);
			switch (tag.EntityType)
			{
				case EntityType.Local:
					{
						SetLocal(node, (InternalLocal)tag, leaveValueOnStack);
						break;
					}
					
				case EntityType.Parameter:
					{
						InternalParameter param = (InternalParameter)tag;
						if (param.Parameter.IsByRef)
						{
							SetByRefParam(param, node.Right, leaveValueOnStack);
							break;
						}
						
						Visit(node.Right);
						EmitCastIfNeeded(param.Type, PopType());
						
						if (leaveValueOnStack)
						{
							_il.Emit(OpCodes.Dup);
							PushType(param.Type);
						}
						_il.Emit(OpCodes.Starg, param.Index);
						break;
					}
					
				case EntityType.Field:
					{
						IField field = (IField)tag;
						SetField(node, field, node.Left, node.Right, leaveValueOnStack);
						break;
					}
					
				case EntityType.Property:
					{
						SetProperty(node, (IProperty)tag, node.Left, node.Right, leaveValueOnStack);
						break;
					}
					
				default:
					{
						NotImplemented(node, tag.ToString());
						break;
					}
			}
			if (!leaveValueOnStack)
			{
				PushVoid();
			}
		}

		private void SetByRefParam(InternalParameter param, Expression right, bool leaveValueOnStack)
		{
			LocalBuilder temp = null;
			IType tempType = null;
			if (leaveValueOnStack)
			{
				Visit(right);
				tempType = PopType();
				temp = StoreTempLocal(tempType);
			}

			LoadParam(param);
			if (temp != null)
			{
				LoadLocal(temp, tempType);
			}
			else
			{
				Visit(right);
			}
			
			EmitCastIfNeeded(param.Type, PopType());
			
			OpCode storecode = GetStoreRefParamCode(param.Type);
			if (IsStobj(storecode)) //passing struct/decimal byref
			{
				_il.Emit(storecode, GetSystemType(param.Type));
			}
			else
			{
				_il.Emit(storecode);
			}

			if (null != temp)
			{
				LoadLocal(temp, tempType);
			}
		}

		void EmitTypeTest(BinaryExpression node)
		{
			Visit(node.Left); PopType();
			
			Type type = null;
			if (NodeType.TypeofExpression == node.Right.NodeType)
			{
				type = GetSystemType(((TypeofExpression)node.Right).Type);
			}
			else
			{
				type = GetSystemType(node.Right);
			}
			_il.Emit(OpCodes.Isinst, type);
		}
		
		void OnTypeTest(BinaryExpression node)
		{
			EmitTypeTest(node);
			
			Label isTrue = _il.DefineLabel();
			Label isFalse = _il.DefineLabel();
			_il.Emit(OpCodes.Brtrue, isTrue);
			_il.Emit(OpCodes.Ldc_I4_0);
			_il.Emit(OpCodes.Br, isFalse);
			_il.MarkLabel(isTrue);
			_il.Emit(OpCodes.Ldc_I4_1);
			_il.MarkLabel(isFalse);
			
			PushBool();
		}
		
		void LoadCmpOperands(BinaryExpression node)
		{
			IType lhs = node.Left.ExpressionType;
			IType rhs = node.Right.ExpressionType;
			
			IType type = TypeSystemServices.GetPromotedNumberType(lhs, rhs);
			Visit(node.Left);
			EmitCastIfNeeded(type, PopType());
			Visit(node.Right);
			EmitCastIfNeeded(type, PopType());
		}
		
		void OnEquality(BinaryExpression node)
		{
			LoadCmpOperands(node);
			_il.Emit(OpCodes.Ceq);
			PushBool();
		}
		
		void OnInequality(BinaryExpression node)
		{
			LoadCmpOperands(node);
			_il.Emit(OpCodes.Ceq);
			EmitIntNot();
			PushBool();
		}
		
		void OnGreaterThan(BinaryExpression node)
		{
			LoadCmpOperands(node);
			_il.Emit(OpCodes.Cgt);
			PushBool();
		}
		
		void OnGreaterThanOrEqual(BinaryExpression node)
		{
			OnLessThan(node);
			EmitIntNot();
		}
		
		void OnLessThan(BinaryExpression node)
		{
			LoadCmpOperands(node);
			_il.Emit(OpCodes.Clt);
			PushBool();
		}
		
		void OnLessThanOrEqual(BinaryExpression node)
		{
			OnGreaterThan(node);
			EmitIntNot();
		}
		
		void OnExponentiation(BinaryExpression node)
		{
			Visit(node.Left);
			EmitCastIfNeeded(TypeSystemServices.DoubleType, PopType());
			Visit(node.Right);
			EmitCastIfNeeded(TypeSystemServices.DoubleType, PopType());
			_il.EmitCall(OpCodes.Call, Math_Pow, null);
			PushType(TypeSystemServices.DoubleType);
		}
		
		void OnArithmeticOperator(BinaryExpression node)
		{
			IType type = node.ExpressionType;
			node.Left.Accept(this); EmitCastIfNeeded(type, PopType());
			node.Right.Accept(this); EmitCastIfNeeded(type, PopType());
			_il.Emit(GetArithmeticOpCode(type, node.Operator));
			PushType(type);
		}
		
		bool EmitToBoolIfNeeded(Expression expression)
		{
			IType type = GetExpressionType(expression);
			if (TypeSystemServices.ObjectType == type ||
			    TypeSystemServices.DuckType == type)
			{
				_il.EmitCall(OpCodes.Call, RuntimeServices_ToBool_Object, null);
				return true;
			}
			if (TypeSystemServices.DecimalType == type)
			{
				_il.EmitCall(OpCodes.Call, RuntimeServices_ToBool_Decimal, null);
				return true;
			}
			return false;
		}
		
		void EmitAnd(BinaryExpression node)
		{
			EmitLogicalOperator(node, OpCodes.Brtrue, OpCodes.Brfalse);
		}
		
		void EmitOr(BinaryExpression node)
		{
			EmitLogicalOperator(node, OpCodes.Brfalse, OpCodes.Brtrue);
		}
		
		void EmitLogicalOperator(BinaryExpression node, OpCode brForValueType, OpCode brForRefType)
		{
			IType type = GetExpressionType(node);
			Visit(node.Left);
			
			IType lhsType = PopType();
			
			if (null != lhsType && lhsType.IsValueType && !type.IsValueType)
			{
				// if boxing, first evaluate the value
				// as it is and then box it...
				Label evalRhs = _il.DefineLabel();
				Label end = _il.DefineLabel();
				
				_il.Emit(OpCodes.Dup);
				EmitToBoolIfNeeded(node.Left);	// may need to convert decimal to bool
				_il.Emit(brForValueType, evalRhs);
				EmitCastIfNeeded(type, lhsType);
				_il.Emit(OpCodes.Br, end);
				
				_il.MarkLabel(evalRhs);
				_il.Emit(OpCodes.Pop);
				Visit(node.Right);
				EmitCastIfNeeded(type, PopType());
				
				_il.MarkLabel(end);
				
			}
			else
			{
				Label end = _il.DefineLabel();
				
				EmitCastIfNeeded(type, lhsType);
				_il.Emit(OpCodes.Dup);
				
				EmitToBoolIfNeeded(node.Left);
				
				_il.Emit(brForRefType, end);
				
				_il.Emit(OpCodes.Pop);
				Visit(node.Right);
				EmitCastIfNeeded(type, PopType());
				_il.MarkLabel(end);
			}
			
			PushType(type);
		}
		
		IType GetExpectedTypeForBitwiseRightOperand(BinaryExpression node)
		{
			switch (node.Operator)
			{
					// BOO-705
				case BinaryOperatorType.ShiftLeft:
				case BinaryOperatorType.ShiftRight:
					return TypeSystemServices.IntType;
			}
			return GetExpressionType(node);
		}
		
		void EmitBitwiseOperator(BinaryExpression node)
		{
			IType type = node.ExpressionType;
			
			Visit(node.Left);
			EmitCastIfNeeded(type, PopType());
			
			Visit(node.Right);
			EmitCastIfNeeded(
				GetExpectedTypeForBitwiseRightOperand(node),
				PopType());
			
			switch (node.Operator)
			{
				case BinaryOperatorType.BitwiseOr:
					{
						_il.Emit(OpCodes.Or);
						break;
					}
					
				case BinaryOperatorType.BitwiseAnd:
					{
						_il.Emit(OpCodes.And);
						break;
					}
					
				case BinaryOperatorType.ExclusiveOr:
					{
						_il.Emit(OpCodes.Xor);
						break;
					}

				case BinaryOperatorType.ShiftLeft:
					{
						_il.Emit(OpCodes.Shl);
						break;
					}
				case BinaryOperatorType.ShiftRight:
					{
						_il.Emit(OpCodes.Shr);
						break;
					}
			}
			
			PushType(type);
		}
		
		override public void OnBinaryExpression(BinaryExpression node)
		{
			switch (node.Operator)
			{
				case BinaryOperatorType.ShiftLeft:
				case BinaryOperatorType.ShiftRight:
				case BinaryOperatorType.ExclusiveOr:
				case BinaryOperatorType.BitwiseAnd:
				case BinaryOperatorType.BitwiseOr:
					{
						EmitBitwiseOperator(node);
						break;
					}
					
				case BinaryOperatorType.Or:
					{
						EmitOr(node);
						break;
					}
					
				case BinaryOperatorType.And:
					{
						EmitAnd(node);
						break;
					}
					
				case BinaryOperatorType.Addition:
				case BinaryOperatorType.Subtraction:
				case BinaryOperatorType.Multiply:
				case BinaryOperatorType.Division:
				case BinaryOperatorType.Modulus:
					{
						OnArithmeticOperator(node);
						break;
					}
					
				case BinaryOperatorType.Exponentiation:
					{
						OnExponentiation(node);
						break;
					}
					
				case BinaryOperatorType.Assign:
					{
						OnAssignment(node);
						break;
					}
					
				case BinaryOperatorType.Equality:
					{
						OnEquality(node);
						break;
					}
					
				case BinaryOperatorType.Inequality:
					{
						OnInequality(node);
						break;
					}
					
				case BinaryOperatorType.GreaterThan:
					{
						OnGreaterThan(node);
						break;
					}
					
				case BinaryOperatorType.LessThan:
					{
						OnLessThan(node);
						break;
					}
					
				case BinaryOperatorType.GreaterThanOrEqual:
					{
						OnGreaterThanOrEqual(node);
						break;
					}
					
				case BinaryOperatorType.LessThanOrEqual:
					{
						OnLessThanOrEqual(node);
						break;
					}
					
				case BinaryOperatorType.ReferenceInequality:
					{
						OnReferenceComparison(node);
						break;
					}
					
				case BinaryOperatorType.ReferenceEquality:
					{
						OnReferenceComparison(node);
						break;
					}
					
				case BinaryOperatorType.TypeTest:
					{
						OnTypeTest(node);
						break;
					}
					
				default:
					{
						OperatorNotImplemented(node);
						break;
					}
			}
		}
		
		void OperatorNotImplemented(BinaryExpression node)
		{
			NotImplemented(node, node.Operator.ToString());
		}
		
		override public void OnTypeofExpression(TypeofExpression node)
		{
			EmitGetTypeFromHandle(GetSystemType(node.Type));
		}
		
		override public void OnCastExpression(CastExpression node)
		{
			IType type = GetType(node.Type);
			Visit(node.Target);
			EmitCastIfNeeded(type, PopType());
			PushType(type);
		}
		
		override public void OnTryCastExpression(TryCastExpression node)
		{
			Type type = GetSystemType(node.Type);
			
			node.Target.Accept(this); PopType();
			_il.Emit(OpCodes.Isinst, type);
			PushType(node.ExpressionType);
		}
		
		void InvokeMethod(IMethod method, MethodInvocationExpression node)
		{
			MethodInfo mi = GetMethodInfo(method);
			if (!InvokeOptimizedMethod(method, mi, node))
			{
				InvokeRegularMethod(method, mi, node);
			}
		}
		
		bool InvokeOptimizedMethod(IMethod method, MethodInfo mi, MethodInvocationExpression node)
		{
			if (Builtins_ArrayTypedConstructor == mi)
			{
				// optimize constructs such as:
				//		array(int, 2)
				IType type = TypeSystemServices.GetReferencedType(node.Arguments[0]);
				if (null != type)
				{
					Visit(node.Arguments[1]);
					EmitCastIfNeeded(TypeSystemServices.IntType, PopType());
					_il.Emit(OpCodes.Newarr, GetSystemType(type));
					PushType(TypeSystemServices.GetArrayType(type, 1));
					return true;
				}
			}
			else if (Builtins_ArrayTypedCollectionConstructor == mi)
			{
				// optimize constructs such as:
				//		array(int, (1, 2, 3))
				//		array(byte, [1, 2, 3, 4])
				IType type = TypeSystemServices.GetReferencedType(node.Arguments[0]);
				if (null != type)
				{
					ListLiteralExpression items = node.Arguments[1] as ListLiteralExpression;
					if (null != items)
					{
						EmitArray(type, items.Items);
						PushType(TypeSystemServices.GetArrayType(type, 1));
						return true;
					}
				}
			}
			return false;
		}
		
		void InvokeRegularMethod(IMethod method, MethodInfo mi, MethodInvocationExpression node)
		{
			IType targetType = null;
			if (!mi.IsStatic)
			{
				targetType = GetTargetObject(node).ExpressionType;
				PushTargetObject(node, mi);
			}
			
			PushArguments(method, node.Arguments);

			// Emit a constrained call if target is a generic parameter
			if (targetType != null && targetType is IGenericParameter)
			{
				_il.Emit(OpCodes.Constrained, GetSystemType(targetType));  
			}
			_il.EmitCall(GetCallOpCode(node, method, mi), mi, null);
			
			PushType(method.ReturnType);
		}

		private void PushTargetObject(MethodInvocationExpression node, MethodInfo mi)
		{
			Expression target = GetTargetObject(node);
			IType targetType = target.ExpressionType;
			
			// If target is a generic parameter, its address must be loaded
			// to allow a constrained method call
			if (targetType is IGenericParameter)
			{
				LoadAddress(target);
			}
	
			else if (targetType.IsValueType)
			{
				if (mi.DeclaringType.IsValueType)
				{
					LoadAddress(target);					
				}
				else
				{
					Visit(node.Target);
					EmitBox(PopType());
				}
			}
			else
			{
				// pushes target reference
				Visit(node.Target);
				PopType();
			}
		}

		private static Expression GetTargetObject(MethodInvocationExpression node)
		{
			MemberReferenceExpression memberRef = node.Target as MemberReferenceExpression;
			
			// Target might be a generic reference expression rather than a member reference expression
			if (memberRef == null) 
			{
				memberRef = ((GenericReferenceExpression)node.Target).Target as MemberReferenceExpression;
			}
			
			return memberRef.Target;
		}

		private OpCode GetCallOpCode(MethodInvocationExpression node, IMethod method, MethodInfo mi)
		{
			if (method.IsStatic) return OpCodes.Call;
			if (NodeType.SuperLiteralExpression == GetTargetObject(node).NodeType) return OpCodes.Call;
			if (IsValueTypeMethodCall(node, method)) return OpCodes.Call;
			return OpCodes.Callvirt;
		}

		private bool IsValueTypeMethodCall(MethodInvocationExpression node, IMethod method)
		{
			IType type = GetTargetObject(node).ExpressionType;
			return type.IsValueType && method.DeclaringType == type;
		}

		void InvokeSuperMethod(IMethod methodInfo, MethodInvocationExpression node)
		{
			IMethod super = ((InternalMethod)methodInfo).Overriden;
			MethodInfo superMI = GetMethodInfo(super);
			if (methodInfo.DeclaringType.IsValueType)
			{
				_il.Emit(OpCodes.Ldarga_S, 0);
			}
			else
			{
				_il.Emit(OpCodes.Ldarg_0); // this
			}
			PushArguments(super, node.Arguments);
			_il.EmitCall(OpCodes.Call, superMI, null);
			PushType(super.ReturnType);
		}
		
		void EmitGetTypeFromHandle(Type type)
		{
			_il.Emit(OpCodes.Ldtoken, type);
			_il.EmitCall(OpCodes.Call, Type_GetTypeFromHandle, null);
			PushType(TypeSystemServices.TypeType);
		}
		
		void OnEval(MethodInvocationExpression node)
		{
			int allButLast = node.Arguments.Count-1;
			for (int i=0; i<allButLast; ++i)
			{
				Visit(node.Arguments[i]);
				DiscardValueOnStack();
			}
			
			Visit(node.Arguments[-1]);
		}
		
		void OnAddressOf(MethodInvocationExpression node)
		{
			MemberReferenceExpression methodRef = (MemberReferenceExpression)node.Arguments[0];
			MethodInfo method = GetMethodInfo((IMethod)GetEntity(methodRef));
			if (method.IsVirtual)
			{
				_il.Emit(OpCodes.Dup);
				_il.Emit(OpCodes.Ldvirtftn, method);
			}
			else
			{
				_il.Emit(OpCodes.Ldftn, method);
			}
			PushType(TypeSystemServices.IntPtrType);
		}
		
		void OnBuiltinFunction(BuiltinFunction function, MethodInvocationExpression node)
		{
			switch (function.FunctionType)
			{
				case BuiltinFunctionType.Switch:
					{
						OnSwitch(node);
						break;
					}
					
				case BuiltinFunctionType.AddressOf:
					{
						OnAddressOf(node);
						break;
					}
					
				case BuiltinFunctionType.Eval:
					{
						OnEval(node);
						break;
					}

				case BuiltinFunctionType.InitValueType:
					{
						OnInitValueType(node);
						break;
					}
					
				default:
					{
						NotImplemented(node, "BuiltinFunction: " + function.FunctionType);
						break;
					}
			}
		}

		private void OnInitValueType(MethodInvocationExpression node)
		{
			Debug.Assert(1 == node.Arguments.Count);

			Expression argument = node.Arguments[0];
			LoadAddressForInitObj(argument);
			System.Type type = GetSystemType(GetExpressionType(argument));
			Debug.Assert(type.IsValueType);
			_il.Emit(OpCodes.Initobj, type);
			PushVoid();
		}

		private void LoadAddressForInitObj(Expression argument)
		{
			IEntity entity = argument.Entity;
			switch (entity.EntityType)
			{
				case EntityType.Local:
					{
						InternalLocal local = (InternalLocal)entity;
						LocalBuilder builder = local.LocalBuilder;
						_il.Emit(OpCodes.Ldloca, builder);
						break;
					}
				case EntityType.Field:
					{
						EmitLoadFieldAddress(argument, (IField)entity);
						break;
					}
				default:
					NotImplemented(argument, "__initobj__");
					break;
			}
		}

		override public void OnMethodInvocationExpression(MethodInvocationExpression node)
		{
			IEntity tag = TypeSystemServices.GetEntity(node.Target);
			switch (tag.EntityType)
			{
				case EntityType.BuiltinFunction:
					{
						OnBuiltinFunction((BuiltinFunction)tag, node);
						break;
					}
					
				case EntityType.Method:
					{
						IMethod methodInfo = (IMethod)tag;
						
						if (node.Target.NodeType == NodeType.SuperLiteralExpression)
						{
							InvokeSuperMethod(methodInfo, node);
						}
						else
						{
							InvokeMethod(methodInfo, node);
						}
						
						break;
					}
					
				case EntityType.Constructor:
					{
						IConstructor constructorInfo = (IConstructor)tag;
						ConstructorInfo ci = GetConstructorInfo(constructorInfo);
						
						if (NodeType.SuperLiteralExpression == node.Target.NodeType || node.Target.NodeType == NodeType.SelfLiteralExpression)
						{
							// super constructor call
							_il.Emit(OpCodes.Ldarg_0);
							PushArguments(constructorInfo, node.Arguments);
							_il.Emit(OpCodes.Call, ci);
							PushVoid();
						}
						else
						{
							PushArguments(constructorInfo, node.Arguments);
							_il.Emit(OpCodes.Newobj, ci);
							
							// constructor invocation resulting type is
							PushType(constructorInfo.DeclaringType);
						}
						break;
					}
					
				default:
					{
						NotImplemented(node, tag.ToString());
						break;
					}
			}
		}
		
		override public void OnTimeSpanLiteralExpression(TimeSpanLiteralExpression node)
		{
			_il.Emit(OpCodes.Ldc_I8, node.Value.Ticks);
			_il.Emit(OpCodes.Newobj, TimeSpan_LongConstructor);
			PushType(TypeSystemServices.TimeSpanType);
		}
		
		override public void OnIntegerLiteralExpression(IntegerLiteralExpression node)
		{
			if (node.IsLong)
			{
				_il.Emit(OpCodes.Ldc_I8, node.Value);
				PushType(TypeSystemServices.LongType);
			}
			else
			{
				switch (node.Value)
				{
					case -1L: _il.Emit(OpCodes.Ldc_I4_M1); break;
										
					case 0L: _il.Emit(OpCodes.Ldc_I4_0); break;					
					case 1L: _il.Emit(OpCodes.Ldc_I4_1); break;
										
					case 2L: _il.Emit(OpCodes.Ldc_I4_2); break;
					case 3L: _il.Emit(OpCodes.Ldc_I4_3); break;
					case 4L: _il.Emit(OpCodes.Ldc_I4_4); break;
					case 5L: _il.Emit(OpCodes.Ldc_I4_5); break;
					case 6L: _il.Emit(OpCodes.Ldc_I4_6); break;
					case 7L: _il.Emit(OpCodes.Ldc_I4_7); break;
					case 8L: _il.Emit(OpCodes.Ldc_I4_8); break;					

					default:
						{
							_il.Emit(OpCodes.Ldc_I4, (int)node.Value);
							break;
						}
				}
				PushType(TypeSystemServices.IntType);
			}
		}
		
		override public void OnDoubleLiteralExpression(DoubleLiteralExpression node)
		{
			if (node.IsSingle)
			{
				_il.Emit(OpCodes.Ldc_R4, (float)node.Value);
				PushType(TypeSystemServices.SingleType);
			}
			else
			{
				_il.Emit(OpCodes.Ldc_R8, node.Value);
				PushType(TypeSystemServices.DoubleType);
			}
		}
		
		override public void OnBoolLiteralExpression(BoolLiteralExpression node)
		{
			if (node.Value)
			{
				_il.Emit(OpCodes.Ldc_I4_1);
			}
			else
			{
				_il.Emit(OpCodes.Ldc_I4_0);
			}
			PushBool();
		}
		
		override public void OnHashLiteralExpression(HashLiteralExpression node)
		{
			_il.Emit(OpCodes.Newobj, Hash_Constructor);
			
			IType objType = TypeSystemServices.ObjectType;
			foreach (ExpressionPair pair in node.Items)
			{
				_il.Emit(OpCodes.Dup);
				
				Visit(pair.First);
				EmitCastIfNeeded(objType, PopType());
				
				Visit(pair.Second);
				EmitCastIfNeeded(objType, PopType());
				_il.EmitCall(OpCodes.Callvirt, Hash_Add, null);
			}
			
			PushType(TypeSystemServices.HashType);
		}
		
		override public void OnGeneratorExpression(GeneratorExpression node)
		{
			NotImplemented(node, node.ToString());
		}
		
		override public void OnListLiteralExpression(ListLiteralExpression node)
		{
			if (node.Items.Count > 0)
			{
				EmitObjectArray(node.Items);
				_il.Emit(OpCodes.Ldc_I4_1);
				_il.Emit(OpCodes.Newobj, List_ArrayBoolConstructor);
			}
			else
			{
				_il.Emit(OpCodes.Newobj, List_EmptyConstructor);
			}
			PushType(TypeSystemServices.ListType);
		}
		
		override public void OnArrayLiteralExpression(ArrayLiteralExpression node)
		{
			IArrayType type = (IArrayType)node.ExpressionType;
			EmitArray(type.GetElementType(), node.Items);
			PushType(type);
		}
		
		override public void OnRELiteralExpression(RELiteralExpression node)
		{
			_il.Emit(OpCodes.Ldstr, RuntimeServices.Mid(node.Value, 1, -1));
			_il.Emit(OpCodes.Newobj, Regex_Constructor);
			PushType(node.ExpressionType);
		}
		
		override public void OnStringLiteralExpression(StringLiteralExpression node)
		{
			if (0 != node.Value.Length)
			{ 
				_il.Emit(OpCodes.Ldstr, node.Value);
			}
			else /* force use of CLR-friendly string.Empty */
			{
				_il.Emit(OpCodes.Ldsfld, typeof(string).GetField("Empty"));
			}
			PushType(TypeSystemServices.StringType);
		}
		
		override public void OnCharLiteralExpression(CharLiteralExpression node)
		{
			_il.Emit(OpCodes.Ldc_I4, node.Value[0]);
			PushType(TypeSystemServices.CharType);
		}
		
		override public void OnSlicingExpression(SlicingExpression node)
		{
			if (AstUtil.IsLhsOfAssignment(node))
			{
				return;
			}
			
			Visit(node.Target);
			
			IArrayType type = (IArrayType)PopType();
			EmitNormalizedArrayIndex(node, node.Indices[0].Begin);
			
			IType elementType = type.GetElementType();
			OpCode opcode = GetLoadEntityOpCode(elementType);
			if (OpCodes.Ldelema.Value == opcode.Value)
			{
				Type systemType = GetSystemType(elementType);
				_il.Emit(opcode, systemType);
				_il.Emit(OpCodes.Ldobj, systemType);
			}
			else
			{
				_il.Emit(opcode);
			}
			PushType(elementType);
		}
		
		void EmitNormalizedArrayIndex(SlicingExpression sourceNode, Expression index)
		{
			bool isNegative = false;
			if (CanBeNegative(index, ref isNegative)
			    && !_rawArrayIndexing
			    && !AstAnnotations.IsRawIndexing(sourceNode))
			{
				if (isNegative)
				{
					_il.Emit(OpCodes.Dup);
					_il.Emit(OpCodes.Ldlen);
					EmitLoadInt(index);
					_il.Emit(OpCodes.Add);
				}
				else
				{
					_il.Emit(OpCodes.Dup);
					EmitLoadInt(index);
					_il.EmitCall(OpCodes.Call, RuntimeServices_NormalizeArrayIndex, null);
				}
			}
			else
			{
				EmitLoadInt(index);
			}
		}
		
		bool CanBeNegative(Expression expression, ref bool isNegative)
		{
			IntegerLiteralExpression integer = expression as IntegerLiteralExpression;
			if (null != integer)
			{
				if (integer.Value >= 0)
				{
					return false;
				}
				isNegative = true;
			}
			return true;
		}
		
		void EmitLoadInt(Expression expression)
		{
			Visit(expression);
			EmitCastIfNeeded(TypeSystemServices.IntType, PopType());
		}
		
		override public void OnExpressionInterpolationExpression(ExpressionInterpolationExpression node)
		{
			Type stringBuilderType = typeof(StringBuilder);
			ConstructorInfo constructor = stringBuilderType.GetConstructor(new Type[0]);
			ConstructorInfo constructorString = stringBuilderType.GetConstructor(new Type[] { typeof(string) });
			MethodInfo appendObject = stringBuilderType.GetMethod("Append", new Type[] { typeof(object) });
			MethodInfo appendString = stringBuilderType.GetMethod("Append", new Type[] { typeof(string) });
			Expression arg0 = node.Expressions[0];
			IType argType = arg0.ExpressionType;

			/* if arg0 is a string, let's call StringBuilder constructor
			 * directly with the string */
			if ( ( typeof(StringLiteralExpression) == arg0.GetType()
				   && ((StringLiteralExpression) arg0).Value.Length > 0 )
				|| ( typeof(StringLiteralExpression) != arg0.GetType()
					 && TypeSystemServices.StringType == argType ) )
			{
				Visit(arg0);
				PopType();
				_il.Emit(OpCodes.Newobj, constructorString);
			}
			else
			{
				_il.Emit(OpCodes.Newobj, constructor);
				arg0 = null; /* arg0 is not a string so we want it to be appended below */
			}
			
			foreach (Expression arg in node.Expressions)
			{
				/* we do not need to append literal string.Empty
				 * or arg0 if it has been handled by ctor */
				if ( ( typeof(StringLiteralExpression) == arg.GetType() 
					   && ((StringLiteralExpression) arg).Value.Length == 0 )
					|| arg == arg0 )
				{
					continue;
				}

				Visit(arg);
				
				argType = PopType();
				if (TypeSystemServices.StringType == argType)
				{
					_il.EmitCall(OpCodes.Call, appendString, null);
				}
				else
				{
					EmitCastIfNeeded(TypeSystemServices.ObjectType, argType);
					_il.EmitCall(OpCodes.Call, appendObject, null);
				}
			}
			_il.EmitCall(OpCodes.Call, stringBuilderType.GetMethod("ToString", new Type[0]), null);
			PushType(TypeSystemServices.StringType);
		}
		
		void LoadMemberTarget(Expression self, IMember member)
		{
			if (member.DeclaringType.IsValueType)
			{
				LoadAddress(self);
			}
			else
			{
				Visit(self);
				PopType();
			}
		}
		
		void EmitLoadFieldAddress(Expression expression, IField field)
		{
			if (field.IsStatic)
			{
				_il.Emit(OpCodes.Ldsflda, GetFieldInfo(field));
			}
			else
			{
				LoadMemberTarget(((MemberReferenceExpression)expression).Target, field);
				_il.Emit(OpCodes.Ldflda, GetFieldInfo(field));
			}
		}
		
		void EmitLoadField(Expression self, IField fieldInfo)
		{
			if (fieldInfo.IsStatic)
			{
				if (fieldInfo.IsLiteral)
				{
					EmitLoadLiteralField(self, fieldInfo);
				}
				else
				{
					_il.Emit(OpCodes.Ldsfld, GetFieldInfo(fieldInfo));
				}
			}
			else
			{
				LoadMemberTarget(self, fieldInfo);
				_il.Emit(OpCodes.Ldfld, GetFieldInfo(fieldInfo));
			}
			PushType(fieldInfo.Type);
		}
		
		object GetStaticValue(IField field)
		{
			InternalField internalField = field as InternalField;
			if (null != internalField)
			{
				return GetInternalFieldStaticValue(internalField);
			}
			return field.StaticValue;
		}
		
		object GetInternalFieldStaticValue(InternalField field)
		{
			return GetValue(field.Type, (Expression)field.StaticValue);
		}
		
		void EmitLoadLiteralField(Node node, IField fieldInfo)
		{
			object value = GetStaticValue(fieldInfo);
			if (null == value)
			{
				_il.Emit(OpCodes.Ldnull);
			}
			else
			{
				TypeCode type = Type.GetTypeCode(value.GetType());
				switch (type)
				{
					case TypeCode.Byte:
						{
							_il.Emit(OpCodes.Ldc_I4, (int)(byte)value);
							_il.Emit(OpCodes.Conv_U1);
							break;
						}
						
					case TypeCode.SByte:
						{
							_il.Emit(OpCodes.Ldc_I4, (int)(sbyte)value);
							_il.Emit(OpCodes.Conv_I1);
							break;
						}
						
					case TypeCode.Char:
						{
							_il.Emit(OpCodes.Ldc_I4, (int)(char)value);
							break;
						}
						
					case TypeCode.Int16:
						{
							_il.Emit(OpCodes.Ldc_I4, (int)(short)value);
							break;
						}

					case TypeCode.UInt16:
						{
							_il.Emit(OpCodes.Ldc_I4, (int)(ushort)value);
							break;
						}
						
					case TypeCode.Int32:
						{
							_il.Emit(OpCodes.Ldc_I4, (int)value);
							break;
						}
						
					case TypeCode.UInt32:
						{
							uint uValue = (uint)value;
							_il.Emit(OpCodes.Ldc_I4, unchecked((int)uValue));
							_il.Emit(OpCodes.Conv_U4);
							break;
						}
						
					case TypeCode.Int64:
						{
							_il.Emit(OpCodes.Ldc_I8, (long)value);
							break;
						}
						
					case TypeCode.UInt64:
						{
							ulong uValue = (ulong)value;
							_il.Emit(OpCodes.Ldc_I8, unchecked((long)uValue));
							_il.Emit(OpCodes.Conv_U8);
							break;
						}
						
					case TypeCode.Single:
						{
							_il.Emit(OpCodes.Ldc_R4, (float)value);
							break;
						}
						
					case TypeCode.Double:
						{
							_il.Emit(OpCodes.Ldc_R8, (double)value);
							break;
						}
						
					case TypeCode.String:
						{
							_il.Emit(OpCodes.Ldstr, (string)value);
							break;
						}
						
					default:
						{
							NotImplemented(node, "Literal: " + type.ToString());
							break;
						}
				}
			}
		}
		
		override public void OnGenericReferenceExpression(GenericReferenceExpression node)
		{
			IEntity tag = TypeSystem.TypeSystemServices.GetEntity(node);
			switch (tag.EntityType)
			{
				case EntityType.Type:
					{
						EmitGetTypeFromHandle(GetSystemType(node));
						break;
					}
					
				case EntityType.Method:
					{
						node.Target.Accept(this);
						break;
					}
					
				default:
					{
						NotImplemented(node, tag.ToString());
						break;
					}
			}
		}
		
		override public void OnMemberReferenceExpression(MemberReferenceExpression node)
		{
			IEntity tag = TypeSystem.TypeSystemServices.GetEntity(node);
			switch (tag.EntityType)
			{
				case EntityType.Method:
					{
						node.Target.Accept(this);
						break;
					}
					
				case EntityType.Field:
					{
						EmitLoadField(node.Target, (IField)tag);
						break;
					}
					
				case EntityType.Type:
					{
						EmitGetTypeFromHandle(GetSystemType(node));
						break;
					}
					
				default:
					{
						NotImplemented(node, tag.ToString());
						break;
					}
			}
		}
		
		void LoadAddress(Expression expression)
		{
			if (NodeType.SelfLiteralExpression == expression.NodeType)
			{
				if (expression.ExpressionType.IsValueType)
				{
					_il.Emit(OpCodes.Ldarg_0);
					return;
				}
			}
			
			IEntity tag = expression.Entity;
			if (null != tag)
			{
				switch (tag.EntityType)
				{
					case EntityType.Local:
						{
							_il.Emit(OpCodes.Ldloca, ((InternalLocal)tag).LocalBuilder);
							return;
						}
						
					case EntityType.Parameter:
						{
							InternalParameter param = (InternalParameter)tag;
							if (param.Parameter.IsByRef)
							{
								LoadParam(param);
							}
							else
							{
								_il.Emit(OpCodes.Ldarga, param.Index);
							}
							return;
						}
						
					case EntityType.Field:
						{
							IField field = (IField)tag;
							if (!field.IsLiteral)
							{
								EmitLoadFieldAddress(expression, field);
								return;
							}
							break;
						}
				}
			}
			
			if (IsValueTypeArraySlicing(expression))
			{
				SlicingExpression slicing = (SlicingExpression)expression;
				Visit(slicing.Target);
				IArrayType arrayType = (IArrayType)PopType();
				EmitNormalizedArrayIndex(slicing, slicing.Indices[0].Begin);
				_il.Emit(OpCodes.Ldelema, GetSystemType(arrayType.GetElementType()));
			}
			else
			{
				// declare local to hold value type
				Visit(expression);
				LocalBuilder temp = _il.DeclareLocal(GetSystemType(PopType()));
				_il.Emit(OpCodes.Stloc, temp);
				_il.Emit(OpCodes.Ldloca, temp);
			}
		}
		
		bool IsValueTypeArraySlicing(Expression expression)
		{
			SlicingExpression slicing = expression as SlicingExpression;
			if (null != slicing)
			{
				IArrayType type = (IArrayType)slicing.Target.ExpressionType;
				return type.GetElementType().IsValueType;
			}
			return false;
		}
		
		override public void OnSelfLiteralExpression(SelfLiteralExpression node)
		{
			_il.Emit(OpCodes.Ldarg_0);
			if (node.ExpressionType.IsValueType)
			{
				_il.Emit(OpCodes.Ldobj, GetSystemType(node.ExpressionType));
			}
			PushType(node.ExpressionType);
		}
		
		override public void OnSuperLiteralExpression(SuperLiteralExpression node)
		{
			_il.Emit(OpCodes.Ldarg_0);
			if (node.ExpressionType.IsValueType)
			{
				_il.Emit(OpCodes.Ldobj, GetSystemType(node.ExpressionType));
			}
			PushType(node.ExpressionType);
		}
		
		override public void OnNullLiteralExpression(NullLiteralExpression node)
		{
			_il.Emit(OpCodes.Ldnull);
			PushType(null);
		}
		
		override public void OnReferenceExpression(ReferenceExpression node)
		{
			IEntity info = TypeSystem.TypeSystemServices.GetEntity(node);
			switch (info.EntityType)
			{
				case EntityType.Local:
					{
						InternalLocal local = (InternalLocal)info;
						LocalBuilder builder = local.LocalBuilder;
						_il.Emit(OpCodes.Ldloc, builder);
						PushType(local.Type);
						break;
					}
					
				case EntityType.Parameter:
					{
						InternalParameter param = (InternalParameter)info;
						LoadParam(param);
						
						if (param.Parameter.IsByRef)
						{
							OpCode code = GetLoadRefParamCode(param.Type);
							if (code.Value == OpCodes.Ldobj.Value)
							{
								_il.Emit(code, GetSystemType(param.Type));
							}
							else {
								_il.Emit(code);
							}
						}
						PushType(param.Type);
						break;
					}
					
				case EntityType.Array:
				case EntityType.Type:
					{
						EmitGetTypeFromHandle(GetSystemType(node));
						break;
					}
					
				default:
					{
						NotImplemented(node, info.ToString());
						break;
					}
					
			}
		}
		
		void LoadParam(InternalParameter param)
		{
			int index = param.Index;
			
			switch (index)
			{
				case 0:
					{
						_il.Emit(OpCodes.Ldarg_0);
						break;
					}
					
				case 1:
					{
						_il.Emit(OpCodes.Ldarg_1);
						break;
					}
					
				case 2:
					{
						_il.Emit(OpCodes.Ldarg_2);
						break;
					}
					
				case 3:
					{
						_il.Emit(OpCodes.Ldarg_3);
						break;
					}
					
				default:
					{
						if (index < 256)
						{
							_il.Emit(OpCodes.Ldarg_S, index);
						}
						else
						{
							_il.Emit(OpCodes.Ldarg, index);
						}
						break;
					}
			}
		}
		void SetLocal(BinaryExpression node, InternalLocal tag, bool leaveValueOnStack)
		{
			node.Right.Accept(this); // leaves type on stack
			
			IType typeOnStack = null;
			
			if (leaveValueOnStack)
			{
				typeOnStack = PeekTypeOnStack();
				_il.Emit(OpCodes.Dup);
			}
			else
			{
				typeOnStack = PopType();
			}
			EmitAssignment(tag, typeOnStack);
		}
		
		void EmitAssignment(InternalLocal tag, IType typeOnStack)
		{
			// todo: assignment result must be type on the left in the
			// case of casting
			LocalBuilder local = tag.LocalBuilder;
			EmitCastIfNeeded(tag.Type, typeOnStack);
			_il.Emit(OpCodes.Stloc, local);
		}
		
		void SetField(Node sourceNode, IField field, Expression reference, Expression value, bool leaveValueOnStack)
		{
			OpCode opSetField = OpCodes.Stsfld;
			if (!field.IsStatic)
			{
				opSetField = OpCodes.Stfld;
				if (null != reference)
				{
					LoadMemberTarget(
						((MemberReferenceExpression)reference).Target,
						field);
				}
			}
			
			value.Accept(this);
			EmitCastIfNeeded(field.Type, PopType());
			
			FieldInfo fi = GetFieldInfo(field);
			LocalBuilder local = null;
			if (leaveValueOnStack)
			{
				_il.Emit(OpCodes.Dup);
				local = _il.DeclareLocal(fi.FieldType);
				_il.Emit(OpCodes.Stloc, local);
			}
			
			_il.Emit(opSetField, fi);
			
			if (leaveValueOnStack)
			{
				_il.Emit(OpCodes.Ldloc, local);
				PushType(field.Type);
			}
		}
		
		void SetProperty(Node sourceNode, IProperty property, Expression reference, Expression value, bool leaveValueOnStack)
		{
			OpCode callOpCode = OpCodes.Call;
			
			MethodInfo setMethod = GetMethodInfo(property.GetSetMethod());
			
			if (null != reference)
			{
				if (!setMethod.IsStatic)
				{
					Expression target = ((MemberReferenceExpression)reference).Target;
					if (setMethod.DeclaringType.IsValueType)
					{
						LoadAddress(target);
					}
					else
					{
						callOpCode = OpCodes.Callvirt;
						target.Accept(this);
						PopType();
					}
				}
			}
			
			value.Accept(this);
			EmitCastIfNeeded(property.Type, PopType());
			
			LocalBuilder local = null;
			if (leaveValueOnStack)
			{
				_il.Emit(OpCodes.Dup);
				local = _il.DeclareLocal(GetSystemType(property.Type));
				_il.Emit(OpCodes.Stloc, local);
			}
			
			_il.EmitCall(callOpCode, setMethod, null);
			
			if (leaveValueOnStack)
			{
				_il.Emit(OpCodes.Ldloc, local);
				PushType(property.Type);
			}
		}
		
		bool EmitDebugInfo(Node node)
		{
			return EmitDebugInfo(node, node);
		}		
		
		private const int _DBG_SYMBOLS_QUEUE_CAPACITY = 5; 
		#if NET_2_0
		private System.Collections.Generic.Queue<LexicalInfo> _dbgSymbols = new System.Collections.Generic.Queue<LexicalInfo>(_DBG_SYMBOLS_QUEUE_CAPACITY);
		#else
		private System.Collections.Queue _dbgSymbols = new System.Collections.Queue(_DBG_SYMBOLS_QUEUE_CAPACITY);
		#endif
		
		bool EmitDebugInfo(Node startNode, Node endNode)
		{
			if (!Parameters.Debug) return false;
			
			LexicalInfo start = startNode.LexicalInfo;
			if (!start.IsValid) return false;

			ISymbolDocumentWriter writer = GetDocumentWriter(start.FullPath);
			if (null == writer) return false;
			
			// ensure there is no duplicate emitted
			if (_dbgSymbols.Contains(start)) {
				_context.TraceInfo("duplicate symbol emit attempt for '{0}' : '{1}'.", start.ToString(), startNode.ToString()); 
				return false;
			}
			if (_dbgSymbols.Count >= _DBG_SYMBOLS_QUEUE_CAPACITY) _dbgSymbols.Dequeue();
			_dbgSymbols.Enqueue(start);

			try
			{
				_il.MarkSequencePoint(writer, start.Line, 0, start.Line+1, 0);
			}
			catch (Exception x)
			{
				Error(CompilerErrorFactory.InternalError(startNode, x));
				return false;
			}

			return true;
		}

		private ISymbolDocumentWriter GetDocumentWriter(string fname)
		{
			ISymbolDocumentWriter writer = GetCachedDocumentWriter(fname);
			if (null != writer) return writer;
			
			writer = _moduleBuilder.DefineDocument(
				fname,
				Guid.Empty,
				Guid.Empty,
				SymDocumentType.Text);
			_symbolDocWriters.Add(fname, writer);
			
			return writer;
		}

		private ISymbolDocumentWriter GetCachedDocumentWriter(string fname)
		{
			return (ISymbolDocumentWriter) _symbolDocWriters[fname];
		}

		bool IsBoolOrInt(IType type)
		{
			return TypeSystemServices.BoolType == type ||
				TypeSystemServices.IntType == type;
		}
		
		void PushArguments(IMethodBase entity, ExpressionCollection args)
		{
			IParameter[] parameters = entity.GetParameters();
			for (int i=0; i<args.Count; ++i)
			{
				IType parameterType = parameters[i].Type;
				Expression arg = args[i];
				/*
				InternalParameter internalparam = parameters[i] as InternalParameter;
				if ((parameterType.IsByRef) ||
					(internalparam != null &&
					internalparam.Parameter.IsByRef)
					)
				 */
				if (parameters[i].IsByRef)
				{
					LoadAddress(arg);
				}
				else
				{
					arg.Accept(this);
					EmitCastIfNeeded(parameterType, PopType());
				}
			}
		}
		
		void EmitObjectArray(ExpressionCollection items)
		{
			EmitArray(TypeSystemServices.ObjectType, items);
		}
		
		void EmitArray(IType type, ExpressionCollection items)
		{
			_il.Emit(OpCodes.Ldc_I4, items.Count);
			_il.Emit(OpCodes.Newarr, GetSystemType(type));
			
			OpCode opcode = GetStoreEntityOpCode(type);
			for (int i=0; i<items.Count; ++i)
			{
				StoreEntity(opcode, i, items[i], type);
			}
		}
		
		bool IsInteger(IType type)
		{
			return type == TypeSystemServices.IntType ||
				type == TypeSystemServices.LongType ||
				type == TypeSystemServices.ByteType;
		}
		
		MethodInfo GetToDecimalConversionMethod(IType type)
		{
			MethodInfo method =
				typeof(Decimal).GetMethod("op_Implicit", new Type[] { GetSystemType(type) });
			
			if (method == null)
			{
				method =
					typeof(Decimal).GetMethod("op_Explicit", new Type[] { GetSystemType(type) });
				if (method == null)
				{
					NotImplemented(string.Format("Numeric promotion for {0} to decimal not implemented!", type));
				}
			}
			return method;
		}
		
		MethodInfo GetFromDecimalConversionMethod(IType type)
		{
			string toType = "To" + type.Name;

			MethodInfo method =
				typeof(Decimal).GetMethod(toType, new Type[] { typeof(Decimal) });
			if (method == null)
			{
				NotImplemented(string.Format("Numeric promotion for decimal to {0} not implemented!", type));
			}
			return method;
		}
		
		OpCode GetArithmeticOpCode(IType type, BinaryOperatorType op)
		{
			if (IsInteger(type) && _checked)
			{
				switch (op)
				{
					case BinaryOperatorType.Addition: return OpCodes.Add_Ovf;
					case BinaryOperatorType.Subtraction: return OpCodes.Sub_Ovf;
					case BinaryOperatorType.Multiply: return OpCodes.Mul_Ovf;
					case BinaryOperatorType.Division: return OpCodes.Div;
					case BinaryOperatorType.Modulus: return OpCodes.Rem;
				}
			}
			else
			{
				switch (op)
				{
					case BinaryOperatorType.Addition: return OpCodes.Add;
					case BinaryOperatorType.Subtraction: return OpCodes.Sub;
					case BinaryOperatorType.Multiply: return OpCodes.Mul;
					case BinaryOperatorType.Division: return OpCodes.Div;
					case BinaryOperatorType.Modulus: return OpCodes.Rem;
				}
			}
			throw new ArgumentException("op");
		}
		
		OpCode GetLoadEntityOpCode(IType tag)
		{
			if (tag.IsValueType)
			{
				if (TypeSystemServices.IntType == tag ||
				    tag.IsEnum)
				{
					return OpCodes.Ldelem_I4;
				}
				if (TypeSystemServices.UIntType == tag)
				{
					return OpCodes.Ldelem_U4;
				}				
				if (TypeSystemServices.LongType == tag)
				{
					return OpCodes.Ldelem_I8;
				}	
				if (TypeSystemServices.SByteType == tag)
				{
					return OpCodes.Ldelem_I1;
				}				
				if (TypeSystemServices.ByteType == tag)
				{
					return OpCodes.Ldelem_U1;
				}
				if (TypeSystemServices.ShortType == tag ||
				    TypeSystemServices.CharType == tag)
				{
					return OpCodes.Ldelem_I2;
				}
				if (TypeSystemServices.UShortType == tag)
				{
					return OpCodes.Ldelem_U2;
				}				
				if (TypeSystemServices.SingleType == tag)
				{
					return OpCodes.Ldelem_R4;
				}
				if (TypeSystemServices.DoubleType == tag)
				{
					return OpCodes.Ldelem_R8;
				}
				//NotImplemented("LoadEntityOpCode(" + tag + ")");
				return OpCodes.Ldelema;
			}
			return OpCodes.Ldelem_Ref;
		}
		
		OpCode GetStoreEntityOpCode(IType tag)
		{
			if (tag.IsValueType)
			{
				if (TypeSystemServices.IntType == tag ||
				    tag.IsEnum)
				{
					return OpCodes.Stelem_I4;
				}
				if (TypeSystemServices.LongType == tag)
				{
					return OpCodes.Stelem_I8;
				}
				if (TypeSystemServices.ByteType == tag)
				{
					return OpCodes.Stelem_I1;
				}
				if (TypeSystemServices.ShortType == tag ||
				    TypeSystemServices.CharType == tag)
				{
					return OpCodes.Stelem_I2;
				}
				if (TypeSystemServices.SingleType == tag)
				{
					return OpCodes.Stelem_R4;
				}
				if (TypeSystemServices.DoubleType == tag)
				{
					return OpCodes.Stelem_R8;
				}
				//NotImplemented("GetStoreEntityOpCode(" + tag + ")");
				return OpCodes.Stobj;
			}
			return OpCodes.Stelem_Ref;
		}
		
		OpCode GetLoadRefParamCode(IType tag)
		{
			if (tag.IsValueType)
			{
				if (TypeSystemServices.IntType == tag ||
				    tag.IsEnum)
				{
					return OpCodes.Ldind_I4;
				}
				if (TypeSystemServices.LongType == tag)
				{
					return OpCodes.Ldind_I8;
				}
				if (TypeSystemServices.ByteType == tag)
				{
					return OpCodes.Ldind_I1;
				}
				if (TypeSystemServices.ShortType == tag ||
				    TypeSystemServices.CharType == tag)
				{
					return OpCodes.Ldind_I2;
				}
				if (TypeSystemServices.SingleType == tag)
				{
					return OpCodes.Ldind_R4;
				}
				if (TypeSystemServices.DoubleType == tag)
				{
					return OpCodes.Ldind_R8;
				}
				if (TypeSystemServices.UShortType == tag)
				{
					return OpCodes.Ldind_U2;
				}
				if (TypeSystemServices.UIntType == tag)
				{
					return OpCodes.Ldind_U4;
				}
				
				return OpCodes.Ldobj;
			}
			return OpCodes.Ldind_Ref;
		}
		
		OpCode GetStoreRefParamCode(IType tag)
		{
			if (tag.IsValueType)
			{
				if (TypeSystemServices.IntType == tag ||
				    tag.IsEnum)
				{
					return OpCodes.Stind_I4;
				}
				if (TypeSystemServices.LongType == tag)
				{
					return OpCodes.Stind_I8;
				}
				if (TypeSystemServices.ByteType == tag)
				{
					return OpCodes.Stind_I1;
				}
				if (TypeSystemServices.ShortType == tag ||
				    TypeSystemServices.CharType == tag)
				{
					return OpCodes.Stind_I2;
				}
				if (TypeSystemServices.SingleType == tag)
				{
					return OpCodes.Stind_R4;
				}
				if (TypeSystemServices.DoubleType == tag)
				{
					return OpCodes.Stind_R8;
				}
				
				return OpCodes.Stobj;
			}
			return OpCodes.Stind_Ref;
		}
		
		bool IsAssignableFrom(IType expectedType, IType actualType)
		{
			return (IsPtr(expectedType) && IsPtr(actualType))
				|| expectedType.IsAssignableFrom(actualType);
		}
		
		bool IsPtr(IType type)
		{
			return (type == TypeSystemServices.IntPtrType)
				|| (type == TypeSystemServices.UIntPtrType);
		}
		
		void EmitCastIfNeeded(IType expectedType, IType actualType)
		{
			if (null == actualType) // see NullLiteralExpression
			{
				return;
			}
			
			if (!IsAssignableFrom(expectedType, actualType))
			{
				IMethod method = TypeSystemServices.FindImplicitConversionOperator(actualType,expectedType);
				if (method != null)
				{
					EmitBoxIfNeeded(method.GetParameters()[0].Type, actualType);
					_il.EmitCall(OpCodes.Call, GetMethodInfo(method), null);
					return;
				}
				if (expectedType.IsValueType)
				{
					if (actualType.IsValueType)
					{
						// numeric promotion
						if (TypeSystemServices.DecimalType == expectedType)
						{
							_il.EmitCall(OpCodes.Call, GetToDecimalConversionMethod(actualType), null);
						}
						else if (TypeSystemServices.DecimalType == actualType)
						{
							_il.EmitCall(OpCodes.Call, GetFromDecimalConversionMethod(expectedType), null);
						}
						else
						{
							_il.Emit(GetNumericPromotionOpCode(expectedType));
						}
					}
					else
					{
						EmitUnbox(expectedType);
					}
				}
				else
				{
					EmitDuckImplicitCastIfNeeded(expectedType, actualType);

					_context.TraceInfo("castclass: expected type='{0}', type on stack='{1}'", expectedType, actualType);
					_il.Emit(OpCodes.Castclass, GetSystemType(expectedType));
				}
			}
			else
			{
				EmitBoxIfNeeded(expectedType, actualType);
			}
		}

		virtual protected void EmitDuckImplicitCastIfNeeded(IType expectedType, IType actualType)
		{
			if (TypeSystemServices.IsDuckType(actualType))
			{
				EmitGetTypeFromHandle(GetSystemType(expectedType));
				PopType();
				_il.EmitCall(OpCodes.Call, RuntimeServices_DuckImplicitCast, null);
			}
		}
		
		virtual protected MethodInfo RuntimeServices_DuckImplicitCast
		{
			get
			{
				return Types.RuntimeServices.GetMethod("DuckImplicitCast", new Type[] { Types.Object, Types.Type });
			}
		}

		private void EmitBoxIfNeeded(IType expectedType, IType actualType)
		{
			if ((actualType.IsValueType && !expectedType.IsValueType) ||
				(actualType is IGenericParameter && !(expectedType is IGenericParameter)))
			{
				EmitBox(actualType);
			}
		}

		void EmitBox(IType type)
		{
			_il.Emit(OpCodes.Box, GetSystemType(type));
		}
		
		void EmitUnbox(IType expectedType)
		{
			string unboxMethodName = GetUnboxMethodName(expectedType);
			if (null != unboxMethodName)
			{
				_il.EmitCall(OpCodes.Call, GetRuntimeMethod(unboxMethodName), null);
			}
			else
			{
				Type type = GetSystemType(expectedType);
				_il.Emit(OpCodes.Unbox, type);
				_il.Emit(OpCodes.Ldobj, type);
			}
		}
		
		MethodInfo GetRuntimeMethod(string methodName)
		{
			return Types.RuntimeServices.GetMethod(methodName);
		}
		
		string GetUnboxMethodName(IType type)
		{
			if (type == TypeSystemServices.ByteType)
			{
				return "UnboxByte";
			}
			else if (type == TypeSystemServices.SByteType)
			{
				return "UnboxSByte";
			}
			else if (type == TypeSystemServices.ShortType)
			{
				return "UnboxInt16";
			}
			else if (type == TypeSystemServices.UShortType)
			{
				return "UnboxUInt16";
			}
			if (type == TypeSystemServices.IntType)
			{
				return "UnboxInt32";
			}
			else if (type == TypeSystemServices.UIntType)
			{
				return "UnboxUInt32";
			}
			else if (type == TypeSystemServices.LongType)
			{
				return "UnboxInt64";
			}
			else if (type == TypeSystemServices.ULongType)
			{
				return "UnboxUInt64";
			}
			else if (type == TypeSystemServices.SingleType)
			{
				return "UnboxSingle";
			}
			else if (type == TypeSystemServices.DoubleType)
			{
				return "UnboxDouble";
			}
			else if (type == TypeSystemServices.DecimalType)
			{
				return "UnboxDecimal";
			}
			else if (type == TypeSystemServices.BoolType)
			{
				return "UnboxBoolean";
			}
			else if (type == TypeSystemServices.CharType)
			{
				return "UnboxChar";
			}
			return null;
		}
		
		OpCode GetNumericPromotionOpCode(IType type)
		{
			if (type == TypeSystemServices.SByteType)
			{
				return _checked ? OpCodes.Conv_Ovf_I1 : OpCodes.Conv_I1;
			}
			else if (type == TypeSystemServices.ByteType)
			{
				return _checked ? OpCodes.Conv_Ovf_U1 : OpCodes.Conv_U1;
			}
			else if (type == TypeSystemServices.ShortType)
			{
				return _checked ? OpCodes.Conv_Ovf_I2 : OpCodes.Conv_I2;
			}
			else if (type == TypeSystemServices.UShortType ||
			         type == TypeSystemServices.CharType)
			{
				return _checked ? OpCodes.Conv_Ovf_U2 : OpCodes.Conv_U2;
			}
			if (type == TypeSystemServices.IntType ||
			    type.IsEnum)
			{
				return _checked ? OpCodes.Conv_Ovf_I4 : OpCodes.Conv_I4;
			}
			else if (type == TypeSystemServices.UIntType)
			{
				return _checked ? OpCodes.Conv_Ovf_U4 :OpCodes.Conv_U4;
			}
			else if (type == TypeSystemServices.LongType)
			{
				return _checked ? OpCodes.Conv_Ovf_I8 : OpCodes.Conv_I8;
			}
			else if (type == TypeSystemServices.ULongType)
			{
				return _checked ? OpCodes.Conv_Ovf_U8 :OpCodes.Conv_U8;
			}
			else if (type == TypeSystemServices.SingleType)
			{
				return OpCodes.Conv_R4;
			}
			else if (type == TypeSystemServices.DoubleType)
			{
				return OpCodes.Conv_R8;
			}
			else
			{
				throw new NotImplementedException(string.Format("Numeric promotion for {0} not implemented!", type));
			}
		}
		
		void StoreEntity(OpCode opcode, int index, Node value, IType elementType)
		{
			_il.Emit(OpCodes.Dup);	// array reference
			_il.Emit(OpCodes.Ldc_I4, index); // element index
			
			bool stobj = IsStobj(opcode); // value type sequence?
			if (stobj)
			{
				Type systemType = GetSystemType(elementType);
				_il.Emit(OpCodes.Ldelema, systemType);
				value.Accept(this);
				EmitCastIfNeeded(elementType, PopType());	// might need to cast to decimal
				_il.Emit(opcode, systemType);
			}
			else
			{
				value.Accept(this);
				EmitCastIfNeeded(elementType, PopType());
				_il.Emit(opcode);
			}
		}
		
		bool IsStobj(OpCode code)
		{
			return OpCodes.Stobj.Value == code.Value;
		}
		
		void DefineAssemblyAttributes()
		{
			foreach (Attribute attribute in _assemblyAttributes)
			{
				_asmBuilder.SetCustomAttribute(GetCustomAttributeBuilder(attribute));
			}
		}
		
		CustomAttributeBuilder CreateDebuggableAttribute()
		{
			return new CustomAttributeBuilder(
				DebuggableAttribute_Constructor,
				new object[] { true, true });
		}
		
		void DefineEntryPoint()
		{
			if (Context.Parameters.GenerateInMemory)
			{
				Context.GeneratedAssembly = _asmBuilder;
			}
			
			if (CompilerOutputType.Library != Parameters.OutputType)
			{
				Method method = ContextAnnotations.GetEntryPoint(Context);
				if (null != method)
				{
					MethodInfo entryPoint = Context.Parameters.GenerateInMemory
						? _asmBuilder.GetType(method.DeclaringType.FullName).GetMethod(method.Name, BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static)
						: GetMethodBuilder(method);
					_asmBuilder.SetEntryPoint(entryPoint, (PEFileKinds)Parameters.OutputType);
				}
				else
				{
					Errors.Add(CompilerErrorFactory.NoEntryPoint());
				}
			}
		}
		
		private static string GetReferenceTypeName(Type t)
		{
			//return t.FullName + "&";
			string name = t.FullName;
			return name.EndsWith("&")
				? name
				: name + "&";
		}
		
		Type[] GetParameterTypes(ParameterDeclarationCollection parameters)
		{
			Type[] types = new Type[parameters.Count];
			for (int i=0; i<types.Length; ++i)
			{
				types[i] = GetSystemType(parameters[i].Type);
				if (parameters[i].IsByRef)
				{
					string typename = GetReferenceTypeName(types[i]);
					Type byreftype = types[i].Assembly.GetType(typename);
					
					if (byreftype == null) //internal type
					{
						byreftype = _moduleBuilder.GetType(typename,true);
						//TODO ? - test that nested types work too
						//GetTypeBuilder(parameters[i].Type).GetNestedType(typename);
					}
					types[i] = byreftype;
				}
			}
			return types;
		}
		
		Hashtable _builders = new Hashtable();
		
		void SetBuilder(Node node, object builder)
		{
			if (null == builder)
			{
				throw new ArgumentNullException("type");
			}
			_builders[node] = builder;
		}
		
		object GetBuilder(Node node)
		{
			return _builders[node];
		}
		
		internal TypeBuilder GetTypeBuilder(Node node)
		{
			return (TypeBuilder)_builders[node];
		}
		
		PropertyBuilder GetPropertyBuilder(Node node)
		{
			return (PropertyBuilder)_builders[node];
		}
		
		FieldBuilder GetFieldBuilder(Node node)
		{
			return (FieldBuilder)_builders[node];
		}
		
		MethodBuilder GetMethodBuilder(Method method)
		{
			return (MethodBuilder)_builders[method];
		}
		
		ConstructorBuilder GetConstructorBuilder(Method method)
		{
			return (ConstructorBuilder)_builders[method];
		}
		
		LocalBuilder GetLocalBuilder(Node local)
		{
			return GetInternalLocal(local).LocalBuilder;
		}
		
		PropertyInfo GetPropertyInfo(IEntity tag)
		{
			ExternalProperty external = tag as ExternalProperty;
			if (null != external)
			{
				return external.PropertyInfo;
			}
			return GetPropertyBuilder(((InternalProperty)tag).Property);
		}
		
		FieldInfo GetFieldInfo(IField tag)
		{
#if NET_2_0
			// If field is mapped from a generic type, get its mapped FieldInfo
			// on the constructed type
			MixedGenericType.MappedField mapped = tag as MixedGenericType.MappedField;
			if (null != mapped && mapped.FieldInfo.DeclaringType.IsGenericType)
			{
				return MapGenericField(mapped.DeclaringType, mapped.FieldInfo);
			}
#endif
			ExternalField external = tag as ExternalField;
			if (null != external)
			{
				return external.FieldInfo;
			}
			return GetFieldBuilder(((InternalField)tag).Field);
		}
		
		MethodInfo GetMethodInfo(IMethod entity)
		{
#if NET_2_0
			// If method is mapped from a generic type, get its mapped MethodInfo
			// on the constructed type
			MixedGenericType.MappedMethod mapped = entity as MixedGenericType.MappedMethod;
			if (null != mapped && mapped.MethodInfo.DeclaringType.IsGenericType)
			{
				return MapGenericMethod(mapped.DeclaringType, (MethodInfo)mapped.MethodInfo);
			}
			
			// If method is a mixed or internal constructed generic method, 
			// get its mapped MethodInfo
			if (entity is MixedGenericMethod || entity is InternalGenericMethod)
			{
				return MapGenericMethod(entity.GenericMethodInfo);
			}			
#endif
			ExternalMethod external = entity as ExternalMethod;
			if (null != external)
			{
				return (MethodInfo)external.MethodInfo;
			}
			
			return GetMethodBuilder(((InternalMethod)entity).Method);
		}

		ConstructorInfo GetConstructorInfo(IConstructor entity)
		{
#if NET_2_0
			// If constructor is mapped from a generic type, get its mapped ConstructorInfo
			MixedGenericType.MappedConstructor mapped = entity as MixedGenericType.MappedConstructor;
			if (null != mapped && mapped.ConstructorInfo.DeclaringType.IsGenericType)
			{
				return MapGenericConstructor(mapped.DeclaringType, mapped.ConstructorInfo);
			}
#endif
			ExternalConstructor external = entity as ExternalConstructor;
			if (null != external)
			{
				return external.ConstructorInfo;
			}
			
			return GetConstructorBuilder(((InternalMethod)entity).Method);
		}
		
#if NET_2_0
		/// <summary>
		/// Maps a method declared on a generic type definition or an open constructed type
		/// to the corresponding method on a closed constructed type.
		/// </summary>
		private MethodInfo MapGenericMethod(IType targetType, MethodInfo method)
		{
			if (!method.DeclaringType.IsGenericTypeDefinition)
			{
				// HACK: .NET Reflection doesn't allow calling TypeBuilder.GetMethod(Type, MethodInfo)
				// on types that aren't generic definitions, so we have to manually find the
				// corresponding MethodInfo on the declaring type's definition before mapping it
				Type definition = method.DeclaringType.GetGenericTypeDefinition();
				method = Array.Find<MethodInfo>(
					definition.GetMethods(),
					delegate(MethodInfo mi) { return mi.MetadataToken == method.MetadataToken; });
			}
			
			return TypeBuilder.GetMethod(GetSystemType(targetType), method);
		}
		
		/// <summary>
		/// Maps a generic method to its constructed version.
		/// </summary>
		private MethodInfo MapGenericMethod(IGenericMethodInfo genericMethodInfo)
		{
			Type[] arguments = Array.ConvertAll<IType, Type>(
				genericMethodInfo.GenericArguments,
				GetSystemType);
				
			return GetMethodInfo(genericMethodInfo.GenericDefinition).MakeGenericMethod(arguments);
		}

		/// <summary>
		/// Maps a field declared on a generic type definition or an open constructed type
		/// to the corresponding field on a closed constructed type.
		/// </summary>
		private FieldInfo MapGenericField(IType targetType, FieldInfo field)
		{
			if (!field.DeclaringType.IsGenericTypeDefinition)
			{
				// HACK: .NET Reflection doesn't allow calling TypeBuilder.GetMethod(Type, FieldInfo)
				// on types that aren't generic definitions, so we have to manually find the
				// corresponding FieldInfo on the declaring type's definition before mapping it
				Type definition = field.DeclaringType.GetGenericTypeDefinition();
				field = definition.GetField(field.Name);
			}
			return TypeBuilder.GetField(GetSystemType(targetType), field);
		}

		/// <summary>
		/// Maps a constructor declared on a generic type definition or an open constructed type
		/// to the corresponding constructor on a closed constructed type.
		/// </summary>
		private ConstructorInfo MapGenericConstructor(IType targetType, ConstructorInfo ctor)
		{
			if (!ctor.DeclaringType.IsGenericTypeDefinition)
			{
				// HACK: .NET Reflection doesn't allow calling
				// TypeBuilder.GetConstructor(Type, ConstructorInfo) on types that aren't generic
				// definitions, so we have to manually find the corresponding ConstructorInfo on the
				// declaring type's definition before mapping it
				Type definition = ctor.DeclaringType.GetGenericTypeDefinition();
				ctor = Array.Find<ConstructorInfo>(
					definition.GetConstructors(),
					delegate(ConstructorInfo ci) { return ci.MetadataToken == ctor.MetadataToken; });
			}

			return TypeBuilder.GetConstructor(GetSystemType(targetType), ctor);
		}
#endif
		
		Type GetSystemType(Node node)
		{
			return GetSystemType(GetType(node));
		}
		
		Type GetSystemType(IType tag)
		{
			Type type = (Type)_typeCache[tag];
			if (type != null)
			{
				return type;
			}
			
			ExternalType external = tag as ExternalType;
			if (null != external)
			{
				MixedGenericType mixed = tag as MixedGenericType;
				if (null != mixed)
				{
					Type[] arguments = new Type[mixed.GenericArguments.Length];
					for (int i = 0; i < arguments.Length; i++)
					{
						arguments[i] = GetSystemType(mixed.GenericArguments[i]);
					}
					type = GetSystemType(mixed.GenericDefinition).MakeGenericType(arguments);
				}
				else
				{
					type = external.ActualType;
				}
			}
			else if (tag.IsArray)
			{
				IArrayType arrayType = (IArrayType)tag;
				Type systemType = GetSystemType(arrayType.GetElementType());
				int rank = arrayType.GetArrayRank();
				
				if (rank == 1)
				{
					type = systemType.MakeArrayType();
				}
				else
				{
					type = systemType.MakeArrayType(rank);
				}
			}
			else if (Null.Default == tag)
			{
				type = Types.Object;
			}
			else if (tag is InternalGenericParameter)
			{
				return (Type)GetBuilder(((InternalGenericParameter)tag).Node);
			}
			else if (tag is AbstractInternalType)
			{
				type = (Type)GetBuilder(((AbstractInternalType)tag).TypeDefinition);
			}

			if (null == type)
			{
				throw new InvalidOperationException(string.Format("Could not find a Type for {0}.", tag));
			}

			_typeCache.Add(tag, type);
			
			return type;
		}
		
		TypeAttributes GetNestedTypeAttributes(TypeMember type)
		{
			return GetExtendedTypeAttributes(GetNestedTypeAccessibility(type), type);
		}
		
		TypeAttributes GetNestedTypeAccessibility(TypeMember type)
		{
			if (type.IsPublic) return TypeAttributes.NestedPublic;
			if (type.IsInternal) return TypeAttributes.NestedAssembly;
			return TypeAttributes.NestedPrivate;
		}
		
		TypeAttributes GetTypeAttributes(TypeMember type)
		{
			TypeAttributes attributes = type.IsPublic ? TypeAttributes.Public : TypeAttributes.NotPublic;
			return GetExtendedTypeAttributes(attributes, type);
		}
		
		TypeAttributes GetExtendedTypeAttributes(TypeAttributes attributes, TypeMember type)
		{
			switch (type.NodeType)
			{
				case NodeType.ClassDefinition:
					{
						attributes |= (TypeAttributes.AnsiClass | TypeAttributes.AutoLayout);
						attributes |= TypeAttributes.Class;
						attributes |= TypeAttributes.BeforeFieldInit;
						
						if (!type.IsTransient)
						{
							attributes |= TypeAttributes.Serializable;
						}
						if (type.IsAbstract)
						{
							attributes |= TypeAttributes.Abstract;
						}
						if (type.IsFinal)
						{
							attributes |= TypeAttributes.Sealed;
						}
						if (((IType)type.Entity).IsValueType)
						{
							attributes |= TypeAttributes.SequentialLayout;
						}
						break;
					}
					
				case NodeType.EnumDefinition:
					{
						attributes |= TypeAttributes.Sealed;
						attributes |= TypeAttributes.Serializable;
						break;
					}
					
				case NodeType.InterfaceDefinition:
					{
						attributes |= (TypeAttributes.Interface | TypeAttributes.Abstract);
						break;
					}
					
				case NodeType.Module:
					{
						attributes |= TypeAttributes.Sealed;
						break;
					}
			}
			return attributes;
		}
		
		PropertyAttributes GetPropertyAttributes(Property property)
		{
			PropertyAttributes attributes = PropertyAttributes.None;

			if (property.ExplicitInfo != null)
			{
				attributes |= PropertyAttributes.SpecialName | PropertyAttributes.RTSpecialName;
			}
			return attributes;
		}
		
		MethodAttributes GetMethodAttributesFromTypeMember(TypeMember member)
		{
			MethodAttributes attributes = (MethodAttributes)0;
			if (member.IsPublic)
			{
				attributes = MethodAttributes.Public;
			}
			else if (member.IsProtected)
			{
				attributes = member.IsInternal
					? MethodAttributes.FamORAssem
					: MethodAttributes.Family;
			}
			else if (member.IsPrivate)
			{
				attributes = MethodAttributes.Private;
			}
			else if (member.IsInternal)
			{
				attributes = MethodAttributes.Assembly;
			}
			if (member.IsStatic)
			{
				attributes |= MethodAttributes.Static;
				
				if (member.Name.StartsWith("op_"))
				{
					attributes |= MethodAttributes.SpecialName;
				}
			}
			if (member.IsFinal)
			{
				attributes |= MethodAttributes.Final;
			}
			if (member.IsAbstract)
			{
				attributes |= (MethodAttributes.Abstract | MethodAttributes.Virtual);
			}
			if (member.IsVirtual || member.IsOverride)
			{
				attributes |= MethodAttributes.Virtual;
			}
			return attributes;
		}
		
		MethodAttributes GetPropertyMethodAttributes(TypeMember property)
		{
			MethodAttributes attributes = MethodAttributes.SpecialName | MethodAttributes.HideBySig;
			attributes |= GetMethodAttributesFromTypeMember(property);
			return attributes;
		}
		
		MethodAttributes GetMethodAttributes(Method method)
		{
			MethodAttributes attributes = MethodAttributes.HideBySig;
			if (method.ExplicitInfo != null)
			{
				attributes |= MethodAttributes.NewSlot;
			}
			if (IsPInvoke(method))
			{
				Debug.Assert(method.IsStatic);
				attributes |= MethodAttributes.PinvokeImpl;
			}
			attributes |= GetMethodAttributesFromTypeMember(method);
			return attributes;
		}
		
		FieldAttributes GetFieldAttributes(Field field)
		{
			FieldAttributes attributes = 0;
			if (field.IsProtected)
			{
				attributes |= FieldAttributes.Family;
			}
			else if (field.IsPublic)
			{
				attributes |= FieldAttributes.Public;
			}
			else if (field.IsPrivate)
			{
				attributes |= FieldAttributes.Private;
			}
			else if (field.IsInternal)
			{
				attributes |= FieldAttributes.Assembly;
			}
			if (field.IsStatic)
			{
				attributes |= FieldAttributes.Static;
			}
			if (field.IsTransient)
			{
				attributes |= FieldAttributes.NotSerialized;
			}
			if (field.IsFinal)
			{
				IField entity = (IField)field.Entity;
				if (entity.IsLiteral)
				{
					attributes |= FieldAttributes.Literal;
				}
				else
				{
					attributes |= FieldAttributes.InitOnly;
				}
			}
			return attributes;
		}
		
		ParameterAttributes GetParameterAttributes(ParameterDeclaration param)
		{
			return ParameterAttributes.None;
		}
		
		void DefineEvent(TypeBuilder typeBuilder, Event node)
		{
			EventBuilder builder = typeBuilder.DefineEvent(node.Name,
			                                               EventAttributes.None,
			                                               GetSystemType(node.Type));
			//MethodAttributes attribs = GetPropertyMethodAttributes(node);
			MethodAttributes baseAttributes = MethodAttributes.SpecialName;
			builder.SetAddOnMethod(DefineMethod(typeBuilder, node.Add, baseAttributes|GetMethodAttributes(node.Add)));
			builder.SetRemoveOnMethod(DefineMethod(typeBuilder, node.Remove, baseAttributes|GetMethodAttributes(node.Remove)));

			if (null != node.Raise)
			{
				builder.SetRaiseMethod(DefineMethod(typeBuilder, node.Raise, baseAttributes|GetMethodAttributes(node.Raise)));
			}

			SetBuilder(node, builder);
		}
		
		void DefineProperty(TypeBuilder typeBuilder, Property property)
		{
			string name;
			if (property.ExplicitInfo != null)
			{
				name = property.ExplicitInfo.InterfaceType.Name + "." + property.Name;
			}
			else
			{
				name = property.Name;
			}

			PropertyBuilder builder = typeBuilder.DefineProperty(name,
			                                                     GetPropertyAttributes(property),
			                                                     GetSystemType(property.Type),
			                                                     GetParameterTypes(property.Parameters));
			Method getter = property.Getter;
			Method setter = property.Setter;
			
			MethodAttributes attribs = GetPropertyMethodAttributes(property);
			if (null != getter)
			{
				MethodBuilder getterBuilder =
					DefineMethod(typeBuilder, getter, attribs);
				builder.SetGetMethod(getterBuilder);
			}
			if (null != setter)
			{
				MethodBuilder setterBuilder =
					DefineMethod(typeBuilder, setter, attribs);
				builder.SetSetMethod(setterBuilder);
			}
			bool isDuckTyped = GetEntity(property).IsDuckTyped;
			if (isDuckTyped)
			{
				builder.SetCustomAttribute(CreateDuckTypedCustomAttribute());
			}
			
			SetBuilder(property, builder);
		}
		
		void DefineField(TypeBuilder typeBuilder, Field field)
		{
			FieldBuilder builder = typeBuilder.DefineField(field.Name,
			                                               GetSystemType(field),
			                                               GetFieldAttributes(field));
			SetBuilder(field, builder);
		}
		
		delegate ParameterBuilder ParameterFactory(int index, System.Reflection.ParameterAttributes attributes, string name);
		
		void DefineParameters(ConstructorBuilder builder, ParameterDeclarationCollection parameters)
		{
			DefineParameters(parameters, new ParameterFactory(builder.DefineParameter));
		}
		
		void DefineParameters(MethodBuilder builder, ParameterDeclarationCollection parameters)
		{
			DefineParameters(parameters, new ParameterFactory(builder.DefineParameter));
		}
		
		void DefineParameters(ParameterDeclarationCollection parameters, ParameterFactory defineParameter)
		{
			int last = parameters.Count - 1;
			for (int i=0; i<parameters.Count; ++i)
			{
				ParameterBuilder paramBuilder = defineParameter(i+1, GetParameterAttributes(parameters[i]), parameters[i].Name);
				if (last == i && parameters.VariableNumber)
				{
					SetParamArrayAttribute(paramBuilder);
				}
				SetBuilder(parameters[i], paramBuilder);
			}
		}
		
		void SetParamArrayAttribute(ParameterBuilder builder)
		{
			builder.SetCustomAttribute(
				new CustomAttributeBuilder(
					ParamArrayAttribute_Constructor,
					new object[0]));
			
		}
		
		MethodImplAttributes GetImplementationFlags(Method method)
		{
			MethodImplAttributes flags = MethodImplAttributes.Managed;
			if (method.IsRuntime)
			{
				flags |= MethodImplAttributes.Runtime;
			}
			return flags;
		}

		MethodBuilder DefineMethod(TypeBuilder typeBuilder, Method method, MethodAttributes attributes)
		{
			ParameterDeclarationCollection parameters = method.Parameters;
			
			MethodAttributes methodAttributes = GetMethodAttributes(method) | attributes;
			
			string name;
			if (method.ExplicitInfo != null)
			{
				name = method.ExplicitInfo.InterfaceType.Name + "." + method.Name;
			}
			else
			{
				name = method.Name;
			}

			MethodBuilder builder = typeBuilder.DefineMethod(name,
			                                                 methodAttributes);

			if (method.GenericParameters.Count != 0)
			{
				DefineGenericParameters(builder, method.GenericParameters);
			}
			
			builder.SetParameters(GetParameterTypes(parameters));
			builder.SetReturnType(GetSystemType(method.ReturnType));			

			builder.SetImplementationFlags(GetImplementationFlags(method));
			
			DefineParameters(builder, parameters);
			
			SetBuilder(method, builder);
			
			IMethod methodEntity = GetEntity(method);
			if (methodEntity.IsDuckTyped)
			{
				builder.SetCustomAttribute(CreateDuckTypedCustomAttribute());
			}
			return builder;
		}

		void DefineGenericParameters(MethodBuilder builder, GenericParameterDeclarationCollection parameters)
		{
			string[] names = new string[parameters.Count];
			int i = 0;
			
			foreach (GenericParameterDeclaration gpd in parameters)
			{
				names[i] = gpd.Name;
				i++;
			}
			
			builder.DefineGenericParameters(names);

			Type[] parameterBuilders = builder.GetGenericArguments();			
			i = 0;
			foreach (GenericParameterDeclaration gpd in parameters)
			{
				SetBuilder(gpd, parameterBuilders[i++]); 
			}		
		}
		
		private CustomAttributeBuilder CreateDuckTypedCustomAttribute()
		{
			return new CustomAttributeBuilder(DuckTypedAttribute_Constructor, new object[0]);
		}

		void DefineConstructor(TypeBuilder typeBuilder, Method constructor)
		{
			ConstructorBuilder builder = typeBuilder.DefineConstructor(GetMethodAttributes(constructor),
			                                                           CallingConventions.Standard,
			                                                           GetParameterTypes(constructor.Parameters));
			
			builder.SetImplementationFlags(GetImplementationFlags(constructor));
			DefineParameters(builder, constructor.Parameters);
			
			SetBuilder(constructor, builder);
		}
		
		bool IsEnumDefinition(TypeMember type)
		{
			return NodeType.EnumDefinition == type.NodeType;
		}
		
		void DefineType(TypeDefinition typeDefinition)
		{
			TypeBuilder typeBuilder = CreateTypeBuilder(typeDefinition);
			SetBuilder(typeDefinition, typeBuilder);
		}
		
		bool IsValueType(TypeMember type)
		{
			IType entity = type.Entity as IType;
			return null != entity && entity.IsValueType;
		}
		
		TypeBuilder CreateTypeBuilder(TypeMember type)
		{
			Type baseType = null;
			if (IsEnumDefinition(type))
			{
				baseType = typeof(Enum);
			}
			else if (IsValueType(type))
			{
				baseType = Types.ValueType;
			}

			TypeBuilder typeBuilder = null;
			ClassDefinition  enclosingType = type.ParentNode as ClassDefinition;
			if (null == enclosingType)
			{
				typeBuilder = _moduleBuilder.DefineType(type.FullName,
				                                        GetTypeAttributes(type),
				                                        baseType);
			}
			else
			{
				typeBuilder = GetTypeBuilder(enclosingType).DefineNestedType(type.Name,
				                                                             GetNestedTypeAttributes(type),
				                                                             baseType);
			}
			return typeBuilder;
		}
		
		void EmitBaseTypesAndAttributes(TypeDefinition typeDefinition, TypeBuilder typeBuilder)
		{
			foreach (TypeReference baseType in typeDefinition.BaseTypes)
			{
				Type type = GetSystemType(baseType);
				
#if NET_2_0
				// For some reason you can't call IsClass on constructed types created at compile time,
				// so we'll ask the generic definition instead
				if ((type.IsGenericType && type.GetGenericTypeDefinition().IsClass) || (type.IsClass))
#else
					if (type.IsClass)
#endif
				{
					typeBuilder.SetParent(type);
				}
				else
				{
					typeBuilder.AddInterfaceImplementation(type);
				}
			}
		}
		
		void NotImplemented(string feature)
		{
			throw new NotImplementedException(feature);
		}
		
		CustomAttributeBuilder GetCustomAttributeBuilder(Attribute node)
		{
			IConstructor constructor = (IConstructor)GetEntity(node);
			ConstructorInfo constructorInfo = GetConstructorInfo(constructor);
			object[] constructorArgs = GetValues(constructor.GetParameters(),
			                                     node.Arguments);
			
			ExpressionPairCollection namedArgs = node.NamedArguments;
			if (namedArgs.Count > 0)
			{
				PropertyInfo[] namedProperties;
				object[] propertyValues;
				FieldInfo[] namedFields;
				object[] fieldValues;
				GetNamedValues(namedArgs,
				               out namedProperties, out propertyValues,
				               out namedFields, out fieldValues);
				return new CustomAttributeBuilder(
					constructorInfo, constructorArgs,
					namedProperties, propertyValues,
					namedFields, fieldValues);
			}
			return new CustomAttributeBuilder(constructorInfo, constructorArgs);
		}
		
		void GetNamedValues(ExpressionPairCollection values,
		                    out PropertyInfo[] outNamedProperties,
		                    out object[] outPropertyValues,
		                    out FieldInfo[] outNamedFields,
		                    out object[] outFieldValues)
		{
			List namedProperties = new List();
			List propertyValues = new List();
			List namedFields = new List();
			List fieldValues = new List();
			foreach (ExpressionPair pair in values)
			{
				ITypedEntity entity = (ITypedEntity)GetEntity(pair.First);
				object value = GetValue(entity.Type, pair.Second);
				if (EntityType.Property == entity.EntityType)
				{
					namedProperties.Add(GetPropertyInfo(entity));
					propertyValues.Add(value);
				}
				else
				{
					namedFields.Add(GetFieldInfo((IField)entity));
					fieldValues.Add(value);
				}
			}
			
			outNamedProperties = (PropertyInfo[])namedProperties.ToArray(typeof(PropertyInfo));
			outPropertyValues = propertyValues.ToArray();
			outNamedFields = (FieldInfo[])namedFields.ToArray(typeof(FieldInfo));
			outFieldValues = fieldValues.ToArray();
		}
		
		object[] GetValues(IParameter[] targetParameters, ExpressionCollection expressions)
		{
			object[] values = new object[expressions.Count];
			for (int i=0; i<values.Length; ++i)
			{
				values[i] = GetValue(targetParameters[i].Type, expressions[i]);
			}
			return values;
		}
		
		object GetValue(IType expectedType, Expression expression)
		{
			switch (expression.NodeType)
			{
				case NodeType.StringLiteralExpression:
					{
						return ((StringLiteralExpression)expression).Value;
					}
					
				case NodeType.CharLiteralExpression:
					{
						return ((CharLiteralExpression)expression).Value[0];
					}
					
				case NodeType.BoolLiteralExpression:
					{
						return ((BoolLiteralExpression)expression).Value;
					}
					
				case NodeType.IntegerLiteralExpression:
					{
						return ConvertValue(expectedType,
						                    ((IntegerLiteralExpression)expression).Value);
					}
					
				case NodeType.DoubleLiteralExpression:
					{
						return ConvertValue(expectedType,
						                    ((DoubleLiteralExpression)expression).Value);
					}
					
				case NodeType.TypeofExpression:
					{
						return GetSystemType(((TypeofExpression)expression).Type);
					}

				case NodeType.CastExpression:
					{
						return GetValue(expectedType, ((CastExpression)expression).Target);
					}
					
				default:
					{
						IEntity tag = GetEntity(expression);
						if (EntityType.Type == tag.EntityType)
						{
							return GetSystemType(expression);
						}
						else if (EntityType.Field == tag.EntityType)
						{
							IField field = (IField)tag;
							if (field.IsLiteral)
							{
								//Scenario:
								//IF:
								//SomeType.StaticReference = "hamsandwich"
								//[RandomAttribute(SomeType.StaticReferenece)]
								//THEN:
								//field.StaticValue != "hamsandwich"
								//field.StaticValue == SomeType.StaticReference
								//SO:
								//If field.StaticValue is an AST Expression, call GetValue() on it
								if (field.StaticValue is Expression)
								{
									return GetValue(expectedType, field.StaticValue as Expression);
								}
								return field.StaticValue;
							}
						}
						break;
					}
			}
			NotImplemented(expression, "Expression value: " + expression);
			return null;
		}
		
		object ConvertValue(IType expectedType, object value)
		{
			if (expectedType.IsEnum)
			{
				return Convert.ChangeType(value, GetUnderlyingEnumType(expectedType));
			}
			return Convert.ChangeType(value, GetSystemType(expectedType));
		}

		private Type GetUnderlyingEnumType(IType expectedType)
		{
			return expectedType is IInternalEntity
				? Types.Int
				: Enum.GetUnderlyingType(GetSystemType(expectedType));
		}

		void DefineTypeMembers(TypeDefinition typeDefinition)
		{
			if (IsEnumDefinition(typeDefinition))
			{
				return;
			}
			TypeBuilder typeBuilder = GetTypeBuilder(typeDefinition);
			TypeMemberCollection members = typeDefinition.Members;
			foreach (TypeMember member in members)
			{
				switch (member.NodeType)
				{
					case NodeType.Method:
						{
							DefineMethod(typeBuilder, (Method)member, 0);
							break;
						}
						
					case NodeType.Constructor:
						{
							DefineConstructor(typeBuilder, (Constructor)member);
							break;
						}
						
					case NodeType.Field:
						{
							DefineField(typeBuilder, (Field)member);
							break;
						}
						
					case NodeType.Property:
						{
							DefineProperty(typeBuilder, (Property)member);
							break;
						}
						
					case NodeType.Event:
						{
							DefineEvent(typeBuilder, (Event)member);
							break;
						}
				}
			}
		}
		
		string GetAssemblySimpleName(string fname)
		{
			return Path.GetFileNameWithoutExtension(fname);
		}
		
		string GetTargetDirectory(string fname)
		{
			return Path.GetDirectoryName(Path.GetFullPath(fname));
		}
		
		string BuildOutputAssemblyName(string fname)
		{
			if (!Path.HasExtension(fname))
			{
				if (CompilerOutputType.Library == Parameters.OutputType)
				{
					fname += ".dll";
				}
				else
				{
					fname += ".exe";
					
				}
			}
			return Path.GetFullPath(fname);
		}
		
		void DefineResources()
		{
			foreach (ICompilerResource resource in Parameters.Resources)
			{
				resource.WriteResource(_sreResourceService);
			}
		}

		SREResourceService _sreResourceService;

		class SREResourceService : IResourceService
		{
			AssemblyBuilder _asmBuilder;
			ModuleBuilder _moduleBuilder;

			public SREResourceService (AssemblyBuilder asmBuilder, ModuleBuilder modBuilder)
			{
				this._asmBuilder = asmBuilder;
				this._moduleBuilder = modBuilder;
			}

			public bool EmbedFile(string resourceName, string fname)
			{
				MethodInfo embed_res = typeof (AssemblyBuilder).GetMethod(
					"EmbedResourceFile", BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,
					null, CallingConventions.Any, new Type[] { typeof(string), typeof(string) }, null);
				if (embed_res != null)
				{
					embed_res.Invoke(this._asmBuilder, new object[] { resourceName, fname });
					return true;
				}
				return false;
			}

			public IResourceWriter DefineResource(string resourceName, string resourceDescription)
			{
				return this._moduleBuilder.DefineResource(resourceName, resourceDescription);
			}
		}
		
		void SetUpAssembly()
		{
			string fname = Parameters.OutputAssembly;
			if (0 == fname.Length)
			{
				fname = CompileUnit.Modules[0].Name;
			}
			
			string outputFile = BuildOutputAssemblyName(fname);
			
			AssemblyName asmName = CreateAssemblyName(outputFile);
			_asmBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(asmName, GetAssemblyBuilderAccess(), GetTargetDirectory(outputFile));
			if (Parameters.Debug)
			{
				// ikvm tip:  Set DebuggableAttribute to assembly before
				// creating the module, to make sure Visual Studio (Whidbey)
				// picks up the attribute when debugging dynamically generated code.
				_asmBuilder.SetCustomAttribute(CreateDebuggableAttribute());
			}
			_moduleBuilder = _asmBuilder.DefineDynamicModule(asmName.Name, Path.GetFileName(outputFile), Parameters.Debug);
			_sreResourceService = new SREResourceService (_asmBuilder, _moduleBuilder);
			ContextAnnotations.SetAssemblyBuilder(Context, _asmBuilder);
			
			Context.GeneratedAssemblyFileName = outputFile;
		}
		
		AssemblyBuilderAccess GetAssemblyBuilderAccess()
		{
			return Parameters.GenerateInMemory ? AssemblyBuilderAccess.RunAndSave : AssemblyBuilderAccess.Save;
		}
		
		AssemblyName CreateAssemblyName(string outputFile)
		{
			AssemblyName assemblyName = new AssemblyName();
			assemblyName.Name = GetAssemblySimpleName(outputFile);
			assemblyName.Version = GetAssemblyVersion();
			assemblyName.KeyPair = GetAssemblyKeyPair(outputFile);
			return assemblyName;
		}
		
		StrongNameKeyPair GetAssemblyKeyPair(string outputFile)
		{
			Attribute attribute = GetAssemblyAttribute("System.Reflection.AssemblyKeyNameAttribute");
			if (Parameters.KeyContainer != null)
			{
				if (attribute != null)
				{
					Warnings.Add(CompilerWarningFactory.HaveBothKeyNameAndAttribute(attribute));
				}
				if (Parameters.KeyContainer != "")
				{
					return new StrongNameKeyPair(Parameters.KeyContainer);
				}
			}
			else if (attribute != null)
			{
				string asmName = ((StringLiteralExpression)attribute.Arguments[0]).Value;
				if (asmName != "") //ignore empty AssemblyKeyName values, like C# does
				{
					return new StrongNameKeyPair(asmName);
				}
			}
			
			string fname = null;
			string srcFile = null;
			attribute = GetAssemblyAttribute("System.Reflection.AssemblyKeyFileAttribute");
			
			if (Parameters.KeyFile != null)
			{
				fname = Parameters.KeyFile;
				if (attribute != null)
				{
					Warnings.Add(CompilerWarningFactory.HaveBothKeyFileAndAttribute(attribute));
				}
			}
			else if (attribute != null)
			{
				fname = ((StringLiteralExpression)attribute.Arguments[0]).Value;
				if (attribute.LexicalInfo != null)
				{
					srcFile = attribute.LexicalInfo.FileName;
				}
			}
			
			if (null != fname && fname.Length > 0)
			{
				if (!Path.IsPathRooted(fname))
				{
					fname = ResolveRelative(outputFile, srcFile, fname);
				}
				using (FileStream stream = File.OpenRead(fname))
				{
					//Parameters.DelaySign is ignored.
					return new StrongNameKeyPair(stream);
				}
			}
			return null;
		}
		
		string ResolveRelative(string targetFile, string srcFile, string relativeFile)
		{
			//relative to current directory:
			string fname = Path.GetFullPath(relativeFile);
			if (File.Exists(fname))
			{
				return fname;
			}
			
			//relative to source file:
			if (srcFile != null)
			{
				fname = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(srcFile),
				                                      relativeFile));
				if (File.Exists(fname))
				{
					return fname;
				}
			}
			
			//relative to output assembly:
			if (targetFile != null)
			{
				fname = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(targetFile),
				                                      relativeFile));
			}
			return fname;
		}
		
		Version GetAssemblyVersion()
		{
			string version = GetAssemblyAttributeValue("System.Reflection.AssemblyVersionAttribute");
			if (null == version)
			{
				version = "0.0.0.0";
			}
			/* 1.0.* -- BUILD -- based on days since January 1, 2000
			 * 1.0.0.* -- REVISION -- based on seconds since midnight, January 1, 2000, divided by 2			 *
			 */
			string[] sliced = version.Split('.');
			if (sliced.Length > 2)
			{
				DateTime baseTime = new DateTime(2000, 1, 1);
				TimeSpan mark = (DateTime.Now - baseTime);
				if (sliced[2].StartsWith("*"))
				{
					sliced[2] = Math.Round(mark.TotalDays).ToString();
				}
				if (sliced.Length > 3)
				{
					if (sliced[3].StartsWith("*"))
					{
						sliced[3] = Math.Round(mark.TotalSeconds).ToString();
					}
				}
				version = Boo.Lang.Builtins.join(sliced, ".");
			}
			return new Version(version);
		}
		
		string GetAssemblyAttributeValue(string name)
		{
			Attribute attribute = GetAssemblyAttribute(name);
			if (null != attribute)
			{
				return ((StringLiteralExpression)attribute.Arguments[0]).Value;
			}
			return null;
		}
		
		Attribute GetAssemblyAttribute(string name)
		{
			Attribute[] attributes = _assemblyAttributes.Get(name);
			if (attributes.Length > 0)
			{
				Debug.Assert(1 == attributes.Length);
				return attributes[0];
			}
			return null;
		}

		protected override IType GetExpressionType(Expression node)
		{
			IType type = base.GetExpressionType(node);
			if (TypeSystemServices.IsUnknown(type)) throw CompilerErrorFactory.InvalidNode(node);
			return type;
		}
	}
}
